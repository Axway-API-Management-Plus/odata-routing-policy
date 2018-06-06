using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml;
using System.Net;

namespace OdataSwaggerConverter
{
    public static class ExtensionMethods
    {
        public static JObject Responses(this JObject jObject, JObject responses)
        {
            jObject.Add("responses", responses);
            return jObject;
        }

        public static JObject ResponseRef(this JObject responses, string name, string description, string refType)
        {
            responses.Add(name, new JObject()
            {
                {"description", description},
                {
                    "schema", new JObject()
                    {
                        // {"$ref", refType}
                        {"type", "string" }
                    }
                }
            });

            return responses;
        }

        public static JObject Response(this JObject responses, string name, string description, IEdmType type)
        {
            var schema = new JObject();
            Program.SetSwaggerType(schema, type);

            responses.Add(name, new JObject()
            {
                {"description", description},
              //   {"type", "string" }
                /*,
                {"schema", schema}*/
            });

            return responses;
        }

        public static JObject ResponseArrayRef(this JObject responses, string name, string description, string refType)
        {
            responses.Add(name, new JObject()
            {
                {"description", description},
                {
                    "schema", new JObject()
                    {
                        {"type", "array"},
                        {
                            "items", new JObject()
                            {
                                {"$ref", refType}
                            }
                        }
                    }
                }
            });

            return responses;
        }

        public static JObject DefaultErrorResponse(this JObject responses)
        {
            return responses.ResponseRef("default", "Unexpected error", "#/definitions/_Error");
        }

        public static JObject Response(this JObject responses, string name, string description)
        {
            responses.Add(name, new JObject()
            {
                {"description", description},
            });

            return responses;
        }

        public static JObject Parameters(this JObject jObject, JArray parameters)
        {
            jObject.Add("parameters", parameters);

            return jObject;
        }

        public static JArray Parameter(this JArray parameters, string name, string kind, string description,
            string type)
        {
            return Parameter(parameters, name, kind, description, type, format: null, required: null);
        }

        public static JArray Parameter(this JArray parameters, string name, string kind, string description,
            string type, string format = null, bool? required = null)

        {
           parameters.Add(new JObject()
            {
                {"name", name},
                {"in", kind},
                {"description", description},
                {"type", type},
            });

            if (!string.IsNullOrEmpty(format))
            {
                (parameters.Last as JObject).Add("format", format);
            }
            if (required != null)
            {
                (parameters.Last as JObject).Add("required", required);
            }

            return parameters;
        }

        public static JArray Parameter(this JArray parameters, string name, string kind, string description,
            IEdmType type)
        {
            return Parameter(parameters, name, kind, description, type, required: null);
        }

        public static JArray Parameter(this JArray parameters, string name, string kind, string description,
            IEdmType type, bool? required)
        {
            var parameter = new JObject()
            {
                {"name", name},
                {"in", kind},
                {"description", description},
            };

            if (kind != "body")
            {
                Program.SetSwaggerType(parameter, type);
            }
            else
            {
                //Console.WriteLine("Body Type:" + type);
                /*var schema = new JObject();
                Program.SetSwaggerType(schema, type);*/
                var schema = new JObject()
                     {
                {"type", "string"}
                };
                parameter.Add("schema", schema);

            }

            if (required != null)
            {
                parameter.Add("required", required);
            }

            parameters.Add(parameter);

            return parameters;
        }

        public static JArray ParameterRef(this JArray parameters, string name, string kind, string description, string refType)
        {
            parameters.Add(new JObject()
            {
                {"name", name},
                {"in", kind},
                {"description", description}
                ,
                {
                    "schema", new JObject()
                    {
                       // {"$ref", refType}
                        {"type", "string" }
                    }
                }
            });

            return parameters;
        }

        public static JObject Tags(this JObject jObject, params string[] tags)
        {
            jObject.Add("tags", new JArray(tags));

            return jObject;
        }

        public static JObject Summary(this JObject jObject, string summary)
        {
            jObject.Add("summary", summary);

            return jObject;
        }

        public static JObject Description(this JObject jObject, string description)
        {
            jObject.Add("description", description);

            return jObject;
        }

    }
    class Program
    {
        static void Main(string[] args)

        {

            string username = null;
            string password = null;

            if (args.Length < 2)
            {
                Console.WriteLine("Invalid command line arguments");
                Console.WriteLine("Example : OdataSwaggerConverter.exe http://services.odata.org/V3/Northwind/Northwind.svc/$metadata northwind.json");
                return;
            }

            if (args.Length == 4)
            {
                username = args[2];
                password = args[3];
            }


          

            string outputFile = args[1];
            var metadataURI = args[0];

            IEdmModel model = null;
            if (username != null && password != null)
            {

                XmlUrlResolver resolver = new XmlUrlResolver();
                resolver.Credentials = new NetworkCredential(username, password);

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = resolver;
                model = EdmxReader.Parse(System.Xml.XmlTextReader.Create(metadataURI,settings));
            }
            else
            {

               model = EdmxReader.Parse(System.Xml.XmlTextReader.Create(metadataURI));
            }

            
            var url = new Uri(metadataURI);
            var host = url.Host;
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
                   {"description", "The OData Service at " + metadataURI},
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
            // Console.WriteLine(reader.BaseURI);
         
            swaggerDoc.Add("definitions", swaggeDefinitions);

            foreach (var entitySet in model.EntityContainers())
            {

                foreach (var entity in entitySet.EntitySets())
                {

                    swaggerPaths.Add(GetPathForEntity(entity) + "*", CreateSwaggerPathForEntity(entity));
                    swaggerPaths.Add("/" + entity.Name + "*", CreateSwaggerPathForEntitySet(entity));

                }

            }

            foreach (var entitySet in model.EntityContainers())
            {
                foreach (var entity in entitySet.FunctionImports())
                {
                    swaggerPaths.Add(GetPathForOperationImport(entity) + "*", CreateSwaggerPathForOperationImport(entity));
                    swaggerPaths.Add("/" + entity.Name + "*", CreateSwaggerPathForOperationImport(entity));

                    // Console.WriteLine(entity);
                }
            }


            /*foreach (var schemaElement in model.SchemaElements)


            {
             
            }*/




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

        static JObject CreateSwaggerPathForOperationImport(IEdmFunctionImport operationImport)
        {
            JArray swaggerParameters = new JArray();
            foreach (var parameter in operationImport.Parameters)
            {
                swaggerParameters.Parameter(parameter.Name, operationImport is IEdmFunctionImport ? "path" : "body",
                    "parameter: " + parameter.Name, parameter.Type.Definition, required: true);
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
