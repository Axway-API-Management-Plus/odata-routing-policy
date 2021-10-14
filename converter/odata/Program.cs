using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using log4net;


namespace OdataSwaggerConverter
{
   
    class Program
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)

        {
          //  log4net.Config.BasicConfigurator.Configure();
            log.Debug("Converting odatav3 document");
            string samlDisabled = "saml2=disabled";
            bool disableSAML = false;
            string username = null;
            string password = null;

            if (args.Length < 2)
            {
                Console.WriteLine("Invalid command line arguments");
                Console.WriteLine("Example OData 4: OdataSwaggerConverter.exe http://services.odata.org/V4/TripPinServiceRW trip.json");
                Console.WriteLine("Example OData 3: OdataSwaggerConverter.exe http://services.odata.org/V3/Northwind/Northwind.svc/$metadata northwind.json");
                return;
            }

            string outputFile = args[1];
            string metadataURI = args[0];

            if (args.Length == 4)
            {
                username = args[2];
                password = args[3];
            }else if(args.Length == 5 || args.Length == 3)
            {
                username = args[2];
                password = args[3];
                string samlFlag = args[4].Trim();
                if (samlFlag.Equals(samlDisabled))
                {
                    disableSAML = true;
                    metadataURI = metadataURI + "?" + samlDisabled;
                }
            }
        
            try
            {
                var url = new Uri(metadataURI);
                HttpResponseValue httpResponseValue = Util.Get(metadataURI, username, password);
                int statusCode = httpResponseValue.getStatusCode();
                string responseBody = httpResponseValue.getResponseBody();
                string contentType = httpResponseValue.getContentType();

                log.Debug("Response Http Status code : " + statusCode);
                log.Debug("Response from metadata URI : " + responseBody);
                log.Debug("Response Content-Type : " + contentType);

                if (statusCode == 200)
                {
                    if (contentType.Contains("application/json"))
                    {
                        log.Info("Processing odata 4 meta data");
                        OData4.process(url, username, password, httpResponseValue.getResponseBody(), outputFile);

                    }else if (contentType.Contains("application/xml"))
                    {
                        StringReader stringReader = new StringReader(responseBody);
                        IEdmModel model = EdmxReader.Parse(System.Xml.XmlTextReader.Create(stringReader));
                        process(url, model, outputFile, disableSAML);

                    }
                }      
            }catch(Exception e)
            {
                log.Error("Problem in reading meta data file", e);
                return;
            }
        }

        public static void process(Uri url, IEdmModel model, string outputFile, bool disableSAML)
        {
            var host = url.Host;
            int port = url.Port;

            if (!(port == 80 || port == 443))
            {
                host = host + ":" + port;
            }

            var version = "1.0.0";
            var protocol = url.Scheme;
            var basePath = url.AbsolutePath;
            basePath = basePath.Substring(0, basePath.LastIndexOf("/"));
            JObject swaggerDoc = new JObject()
            {
                {"swagger", "2.0"},
                {"info", new JObject()
                {
                    {"title", "OData Service"},
                    {"description", "The OData Service at " + url.ToString()},
                    {"version", version},
                    {"x-odata-version", model.GetMaxDataServiceVersion() + ""}
                }},
                {"host", host},
                {"schemes", new JArray(protocol)},
                {"basePath", basePath},
                {"consumes", new JArray("application/json")},
                {"produces", new JArray("application/json")},
            };

            JObject swaggerPaths = new JObject();
            swaggerDoc.Add("paths", swaggerPaths);
            JObject swaggeDefinitions = new JObject();
            swaggerDoc.Add("definitions", swaggeDefinitions);

            foreach (var entitySet in model.EntityContainers())
            {
                foreach (var entity in entitySet.EntitySets())
                {
                    if (!disableSAML)
                    {
                        swaggerPaths.Add(GetPathForEntity(entity) + "*", CreateSwaggerPathForEntity(entity));
                    }
                    else
                    {
                        swaggerPaths.Add(GetPathForEntity(entity) + "*", CreateSwaggerPathForEntitySAML(entity));
                    }

                    if (!disableSAML)
                    {
                        swaggerPaths.Add("/" + entity.Name + "*", CreateSwaggerPathForEntitySet(entity));
                    }
                    else
                    {
                        swaggerPaths.Add("/" + entity.Name + "*", CreateSwaggerPathForEntitySetSAML(entity));
                    }
                }
            }

            foreach (var entitySet in model.EntityContainers())
            {
                foreach (var entity in entitySet.FunctionImports())
                {
                    swaggerPaths.Add(GetPathForOperationImport(entity) + "*", CreateSwaggerPathForOperationImport(entity, disableSAML));
                    swaggerPaths.Add("/" + entity.Name + "*", CreateSwaggerPathForOperationImport(entity, disableSAML));
                    // Console.WriteLine(entity);
                }
            }
            swaggeDefinitions.Add("_Error", new JObject()
            {
                {
                    "properties", new JObject()
                    {
                        {"error", new JObject()
                        {
                            {"$ref", "#/definitions/_InError"}
                        }
                        }
                    }
                }
            });
            swaggeDefinitions.Add("_InError", new JObject()
            {
                {
                    "properties", new JObject()
                    {
                        {"code", new JObject()
                        {
                            {"type", "string"}
                        }
                        },
                        {"message", new JObject()
                        {
                            {"type", "string"}
                        }
                        }
                    }
                }
            });

            File.WriteAllText(outputFile, swaggerDoc.ToString());
        }


        public static void SetSwaggerType(JObject jObject, IEdmType edmType)
        {
            if (edmType.TypeKind == EdmTypeKind.Complex || edmType.TypeKind == EdmTypeKind.Entity)
            {
                //jObject.Add("$ref", "#/definitions/" + edmType.ToString());
                jObject.Add("type", "string");
            }
            else if (edmType.TypeKind == EdmTypeKind.Primitive)
            {
                string format;
                string type = GetPrimitiveTypeAndFormat((IEdmPrimitiveType)edmType, out format);
                jObject.Add("type", type);
                if (format != null)
                {
                    jObject.Add("format", format);
                }
            }
            else if (edmType.TypeKind == EdmTypeKind.Enum)
            {
                jObject.Add("type", "string");
            }
            else if (edmType.TypeKind == EdmTypeKind.Collection)
            {
                IEdmType itemEdmType = ((IEdmCollectionType)edmType).ElementType.Definition;
                JObject nestedItem = new JObject();
                SetSwaggerType(nestedItem, itemEdmType);
                jObject.Add("type", "array");
                jObject.Add("items", nestedItem);
            }
        }

        static string GetPrimitiveTypeAndFormat(IEdmPrimitiveType primtiveType, out string format)
        {
            format = null;
            switch (primtiveType.PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.String:
                    return "string";
                case EdmPrimitiveTypeKind.Int16:
                case EdmPrimitiveTypeKind.Int32:
                    format = "int32";
                    return "integer";
                case EdmPrimitiveTypeKind.Int64:
                    format = "int64";
                    return "integer";
                case EdmPrimitiveTypeKind.Boolean:
                    return "boolean";
                case EdmPrimitiveTypeKind.Byte:
                    format = "byte";
                    return "string";
                case EdmPrimitiveTypeKind.DateTime:
                    format = "date";
                    return "string";
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    format = "date-time";
                    return "string";
                case EdmPrimitiveTypeKind.Double:
                    format = "double";
                    return "number";
                case EdmPrimitiveTypeKind.Single:
                    format = "float";
                    return "number";
                default:
                    return "string";
            }
        }

        static JObject CreateSwaggerPathForEntitySet(IEdmEntitySet entitySet)

        {
            var keyParameters = new JArray();
            return new JObject()

            {
                {
                    "get", new JObject()
                        .Summary("Get EntitySet " + entitySet.Name)
                        .Description("Returns the EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters(new JArray()
                            .Parameter("$expand", "query", "Expand navigation property", "string")
                            .Parameter("$select", "query", "select structural property", "string")
                            .Parameter("$orderby", "query", "order by some property", "string")
                            .Parameter("$top", "query", "top elements", "integer")
                            .Parameter("$skip", "query", "skip elements", "integer")
                             .Parameter("$inlinecount", "query", "inlcude count in response", "string")
                            .Parameter("$format", "query", "response format", "string")
                            .Parameter("$links", "query", "response format", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "post", new JObject()
                        .Summary("Post a new entity to EntitySet " + entitySet.Name)
                        .Description("Post a new entity to EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters(new JArray()
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to post",
                                entitySet.ElementType)
                        )
                        .Responses(new JObject()
                            .Response("201", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "patch", new JObject()
                        .Summary("Update entity in EntitySet " + entitySet.Name)
                        .Description("Update entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to patch",
                                entitySet.ElementType)
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "delete", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "put", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                }
            };
        }

        static JObject CreateSwaggerPathForEntitySetSAML(IEdmEntitySet entitySet)
        {
            var keyParameters = new JArray();
            return new JObject()
            {
                {
                    "get", new JObject()
                        .Summary("Get EntitySet " + entitySet.Name)
                        .Description("Returns the EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters(new JArray()
                            .Parameter("$expand", "query", "Expand navigation property", "string")
                            .Parameter("$select", "query", "select structural property", "string")
                            .Parameter("$orderby", "query", "order by some property", "string")
                            .Parameter("$top", "query", "top elements", "integer")
                            .Parameter("$skip", "query", "skip elements", "integer")
                             .Parameter("$inlinecount", "query", "inlcude count in response", "string")
                            .Parameter("$format", "query", "response format", "string")
                            .Parameter("$links", "query", "response format", "string")
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "post", new JObject()
                        .Summary("Post a new entity to EntitySet " + entitySet.Name)
                        .Description("Post a new entity to EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters(new JArray()
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to post",
                                entitySet.ElementType)
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("201", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "patch", new JObject()
                        .Summary("Update entity in EntitySet " + entitySet.Name)
                        .Description("Update entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to patch",
                                entitySet.ElementType)
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "delete", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                             .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "put", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                }
            };
        }
        static JObject CreateSwaggerPathForEntity(IEdmEntitySet entitySet)
        {
            var keyParameters = new JArray();
            foreach (var key in entitySet.ElementType.Key())
            {
                string format;
                string type = GetPrimitiveTypeAndFormat(key.Type.Definition as IEdmPrimitiveType, out format);
                bool required = !key.Type.IsNullable;
                keyParameters.Parameter(key.Name, "path", "key: " + key.Name, type, format, required);
            }

            return new JObject()
            {
                {
                    "get", new JObject()
                        .Summary("Get entity from " + entitySet.Name + " by key.")
                        .Description("Returns the entity with the key from " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("$expand", "query", "Expand navigation property", "string")
                            .Parameter("$select", "query", "select structural property", "string")
                            .Parameter("$orderby", "query", "order by some property", "string")
                            .Parameter("$top", "query", "top elements", "integer")
                            .Parameter("$skip", "query", "skip elements", "integer")
                            .Parameter("$inlinecount", "query", "inlcude count in response", "string")
                            .Parameter("$format", "query", "response format", "string")
                            .Parameter("$links", "query", "response format", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "patch", new JObject()
                        .Summary("Update entity in EntitySet " + entitySet.Name)
                        .Description("Update entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to patch",
                                entitySet.ElementType)
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "delete", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },{
                    "put", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                }
            };
        }
        static JObject CreateSwaggerPathForEntitySAML(IEdmEntitySet entitySet)
        {
            var keyParameters = new JArray();
            foreach (var key in entitySet.ElementType.Key())
            {
                string format;
                string type = GetPrimitiveTypeAndFormat(key.Type.Definition as IEdmPrimitiveType, out format);
                bool required = !key.Type.IsNullable;
                keyParameters.Parameter(key.Name, "path", "key: " + key.Name, type, format, required);
            }

            return new JObject()
            {
                {
                    "get", new JObject()
                        .Summary("Get entity from " + entitySet.Name + " by key.")
                        .Description("Returns the entity with the key from " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("$expand", "query", "Expand navigation property", "string")
                            .Parameter("$select", "query", "select structural property", "string")
                            .Parameter("$orderby", "query", "order by some property", "string")
                            .Parameter("$top", "query", "top elements", "integer")
                            .Parameter("$skip", "query", "skip elements", "integer")
                            .Parameter("$inlinecount", "query", "inlcude count in response", "string")
                            .Parameter("$format", "query", "response format", "string")
                            .Parameter("$links", "query", "response format", "string")
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                },
                {
                    "patch", new JObject()
                        .Summary("Update entity in EntitySet " + entitySet.Name)
                        .Description("Update entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter(entitySet.ElementType.Name, "body", "The entity to patch",
                                entitySet.ElementType)
                             .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },
                {
                    "delete", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("204", "Empty response")
                            .DefaultErrorResponse()
                        )
                },{
                    "put", new JObject()
                        .Summary("Delete entity in EntitySet " + entitySet.Name)
                        .Description("Delete entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter("If-Match", "header", "If-Match header", "string")
                            .Parameter("saml2", "query", "Disable SAML SSO", "string")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.ElementType)
                            .DefaultErrorResponse()
                        )
                }
            };
        }

        static string GetPathForEntity(IEdmEntitySet entitySet)
        {
            string singleEntityPath = "/" + entitySet.Name + "(";
            foreach (var key in entitySet.ElementType.Key())
            {
                if (key.Type.Definition.TypeKind == EdmTypeKind.Primitive &&
                    (key.Type.Definition as IEdmPrimitiveType).PrimitiveKind == EdmPrimitiveTypeKind.String)
                {
                    singleEntityPath += "'{" + key.Name + "}', ";
                }
                else
                {
                    singleEntityPath += "{" + key.Name + "}, ";
                }
            }
            singleEntityPath = singleEntityPath.Substring(0, singleEntityPath.Length - 2);
            singleEntityPath += ")";
            //Console.WriteLine("Path for entity :" + singleEntityPath);
            return singleEntityPath;
        }

        static JObject CreateSwaggerPathForOperationImport(IEdmFunctionImport operationImport,  bool disableSAML)
        {
            JArray swaggerParameters = new JArray();
            foreach (var parameter in operationImport.Parameters)
            {
                swaggerParameters.Parameter(parameter.Name, operationImport is IEdmFunctionImport ? "path" : "body",
                    "parameter: " + parameter.Name, parameter.Type.Definition, required: true);
            }

            if (disableSAML)
            {
                swaggerParameters.Parameter("saml2", "query", "Disable SAML SSO", "string");
            }

            JObject swaggerResponses = new JObject();
            if (operationImport.ReturnType == null)
            {
                swaggerResponses.Response("204", "Empty response");
            }
            else
            {
                swaggerResponses.Response("200", "Response from " + operationImport.Name,
                    operationImport.ReturnType.Definition);
            }

            JObject swaggerOperationImport = new JObject()
                .Summary("Call operation import  " + operationImport.Name)
                .Description("Call operation import  " + operationImport.Name)
                .Tags(operationImport is IEdmFunctionImport ? "Function Import" : "Action Import");

            if (swaggerParameters.Count > 0)
            {
                swaggerOperationImport.Parameters(swaggerParameters);
            }
            swaggerOperationImport.Responses(swaggerResponses.DefaultErrorResponse());

            return new JObject()
            {
                {operationImport is IEdmFunctionImport ? "get" : "post", swaggerOperationImport}
            };
        }

        static string GetPathForOperationImport(IEdmFunctionImport operationImport)
        {
            string swaggerOperationImportPath = "/" + operationImport.Name + "(";
            foreach (var parameter in operationImport.Parameters)
            {
                swaggerOperationImportPath += parameter.Name + "=" + "{" + parameter.Name + "},";
            }
            if (swaggerOperationImportPath.EndsWith(","))
            {
                swaggerOperationImportPath = swaggerOperationImportPath.Substring(0,
                    swaggerOperationImportPath.Length - 1);
            }
            swaggerOperationImportPath += ")";
            //Console.WriteLine(swaggerOperationImportPath);
            return swaggerOperationImportPath;  
        }

        static JObject CreateSwaggerDefinitionForStructureType(IEdmStructuredType edmType)
        {
            JObject swaggerProperties = new JObject();
            foreach (var property in edmType.StructuralProperties())
            {
                JObject swaggerProperty = new JObject().Description(property.Name);
                SetSwaggerType(swaggerProperty, property.Type.Definition);
                swaggerProperties.Add(property.Name, swaggerProperty);
            }
            return new JObject()
            {
                {"properties", swaggerProperties}
            };
        }

    }
}
