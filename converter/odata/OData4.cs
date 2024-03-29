﻿using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Linq;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using Newtonsoft.Json.Linq;
using System.Net;
using Newtonsoft.Json;

namespace OdataSwaggerConverter
{
    class OData4
    {
        //private const string metadataURI = "http://services.odata.org/V4/TripPinServiceRW/$metadata";
        // private const string host = "services.odata.org";
        //private const string version = "0.1.0";
        //private const string basePath = "/V4/(S(cnbm44wtbc1v5bgrlek5lpcc))/TripPinServiceRW";
        //  private const string outputFile = @"swagger.json";

        static JObject CreateSwaggerPathForEntitySet(IEdmEntitySet entitySet)
        {
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
                            .Parameter("$count", "query", "inlcude count in response", "boolean")
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.EntityType())
                            .DefaultErrorResponse()
                        )
                },
                {
                    "post", new JObject()
                        .Summary("Post a new entity to EntitySet " + entitySet.Name)
                        .Description("Post a new entity to EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters(new JArray()
                            .Parameter(entitySet.EntityType().Name, "body", "The entity to post",
                                entitySet.EntityType())
                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.EntityType())
                            .DefaultErrorResponse()
                        )
                }
            };
        }

        static JObject CreateSwaggerPathForEntity(IEdmEntitySet entitySet)
        {
            var keyParameters = new JArray();
            foreach (var key in entitySet.EntityType().Key())
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
                            .Parameter("$count", "query", "inlcude count in response", "boolean")

                        )
                        .Responses(new JObject()
                            .Response("200", "EntitySet " + entitySet.Name, entitySet.EntityType())
                            .DefaultErrorResponse()
                        )
                },
                {
                    "patch", new JObject()
                        .Summary("Update entity in EntitySet " + entitySet.Name)
                        .Description("Update entity in EntitySet " + entitySet.Name)
                        .Tags(entitySet.Name)
                        .Parameters((keyParameters.DeepClone() as JArray)
                            .Parameter(entitySet.EntityType().Name, "body", "The entity to patch",
                                entitySet.EntityType())
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
                }
            };
        }

        static string GetPathForEntity(IEdmEntitySet entitySet)
        {
            string singleEntityPath = "/" + entitySet.Name + "(";
            foreach (var key in entitySet.EntityType().Key())
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

            return singleEntityPath;
        }

        static JObject CreateSwaggerPathForOperationImport(IEdmOperationImport operationImport)
        {
            JArray swaggerParameters = new JArray();
            foreach (var parameter in operationImport.Operation.Parameters)
            {
                swaggerParameters.Parameter(parameter.Name, operationImport is IEdmFunctionImport ? "path" : "body",
                    "parameter: " + parameter.Name, parameter.Type.Definition, required: true);
            }

            JObject swaggerResponses = new JObject();
            if (operationImport.Operation.ReturnType == null)
            {
                swaggerResponses.Response("204", "Empty response");
            }
            else
            {
                swaggerResponses.Response("200", "Response from " + operationImport.Name,
                    operationImport.Operation.ReturnType.Definition);
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

        static JObject CreateSwaggerPathForOperationOfEntitySet(IEdmOperation operation, IEdmEntitySet entitySet)
        {
            JArray swaggerParameters = new JArray();
            foreach (var parameter in operation.Parameters.Skip(1))
            {
                swaggerParameters.Parameter(parameter.Name, operation is IEdmFunction ? "path" : "body",
                    "parameter: " + parameter.Name, parameter.Type.Definition, required: true);
            }

            JObject swaggerResponses = new JObject();
            if (operation.ReturnType == null)
            {
                swaggerResponses.Response("204", "Empty response");
            }
            else
            {
                swaggerResponses.Response("200", "Response from " + operation.Name,
                    operation.ReturnType.Definition);
            }

            JObject swaggerOperation = new JObject()
                .Summary("Call operation  " + operation.Name)
                .Description("Call operation  " + operation.Name)
                .Tags(entitySet.Name, operation is IEdmFunction ? "Function" : "Action");


            if (swaggerParameters.Count > 0)
            {
                swaggerOperation.Parameters(swaggerParameters);
            }
            swaggerOperation.Responses(swaggerResponses.DefaultErrorResponse());
            return new JObject()
                {
                    {operation is IEdmFunction ? "get" : "post", swaggerOperation}
                };
        }

        static JObject CreateSwaggerPathForOperationOfEntity(IEdmOperation operation, IEdmEntitySet entitySet)
        {
            JArray swaggerParameters = new JArray();

            foreach (var key in entitySet.EntityType().Key())
            {
                string format;
                string type = GetPrimitiveTypeAndFormat(key.Type.Definition as IEdmPrimitiveType, out format);
                bool required = !key.Type.IsNullable;
                swaggerParameters.Parameter(key.Name, "path", "key: " + key.Name, type, format, required);
            }

            foreach (var parameter in operation.Parameters.Skip(1))
            {
                swaggerParameters.Parameter(parameter.Name, operation is IEdmFunction ? "path" : "body",
                    "parameter: " + parameter.Name, parameter.Type.Definition, required: true);
            }

            JObject swaggerResponses = new JObject();
            if (operation.ReturnType == null)
            {
                swaggerResponses.Response("204", "Empty response");
            }
            else
            {
                swaggerResponses.Response("200", "Response from " + operation.Name,
                    operation.ReturnType.Definition);
            }

            JObject swaggerOperation = new JObject()
                .Summary("Call operation  " + operation.Name)
                .Description("Call operation  " + operation.Name)
                .Tags(entitySet.Name, operation is IEdmFunction ? "Function" : "Action");

            if (swaggerParameters.Count > 0)
            {
                swaggerOperation.Parameters(swaggerParameters);
            }
            swaggerOperation.Responses(swaggerResponses.DefaultErrorResponse());
            return new JObject()
                {
                    {operation is IEdmFunction ? "get" : "post", swaggerOperation}
                };
        }

        static string GetPathForOperationImport(IEdmOperationImport operationImport)
        {
            string swaggerOperationImportPath = "/" + operationImport.Name + "(";
            if (operationImport.IsFunctionImport())
            {
                foreach (var parameter in operationImport.Operation.Parameters)
                {
                    swaggerOperationImportPath += parameter.Name + "=" + "{" + parameter.Name + "},";
                }
            }
            if (swaggerOperationImportPath.EndsWith(","))
            {
                swaggerOperationImportPath = swaggerOperationImportPath.Substring(0,
                    swaggerOperationImportPath.Length - 1);
            }
            swaggerOperationImportPath += ")";

            return swaggerOperationImportPath;
        }

        static string GetPathForOperationOfEntitySet(IEdmOperation operation, IEdmEntitySet entitySet)
        {
            string swaggerOperationPath = "/" + entitySet.Name + "/" + operation.FullName() + "(";
            if (operation.IsFunction())
            {
                foreach (var parameter in operation.Parameters.Skip(1))
                {
                    if (parameter.Type.Definition.TypeKind == EdmTypeKind.Primitive &&
                   (parameter.Type.Definition as IEdmPrimitiveType).PrimitiveKind == EdmPrimitiveTypeKind.String)
                    {
                        swaggerOperationPath += parameter.Name + "=" + "'{" + parameter.Name + "}',";
                    }
                    else
                    {
                        swaggerOperationPath += parameter.Name + "=" + "{" + parameter.Name + "},";
                    }
                }
            }
            if (swaggerOperationPath.EndsWith(","))
            {
                swaggerOperationPath = swaggerOperationPath.Substring(0,
                    swaggerOperationPath.Length - 1);
            }
            swaggerOperationPath += ")";

            return swaggerOperationPath;
        }

        static string GetPathForOperationOfEntity(IEdmOperation operation, IEdmEntitySet entitySet)
        {
            string swaggerOperationPath = GetPathForEntity(entitySet) + "/" + operation.FullName() + "(";
            if (operation.IsFunction())
            {
                foreach (var parameter in operation.Parameters.Skip(1))
                {
                    if (parameter.Type.Definition.TypeKind == EdmTypeKind.Primitive
                        && (parameter.Type.Definition as IEdmPrimitiveType).PrimitiveKind == EdmPrimitiveTypeKind.String)
                    {
                        swaggerOperationPath += parameter.Name + "=" + "'{" + parameter.Name + "}',";
                    }
                    else
                    {
                        swaggerOperationPath += parameter.Name + "=" + "{" + parameter.Name + "},";
                    }
                }
            }
            if (swaggerOperationPath.EndsWith(","))
            {
                swaggerOperationPath = swaggerOperationPath.Substring(0,
                    swaggerOperationPath.Length - 1);
            }
            swaggerOperationPath += ")";

            return swaggerOperationPath;
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

       
        static void Main(string[] args)
        {
            string username = null;
            string password = null;

            if (args.Length < 2)
            {
                Console.WriteLine("Invalid command line arguments");
                Console.WriteLine("Example : OData2Swagger.exe http://services.odata.org/V4/TripPinServiceRW trip.json");
                return;
            }

            if (args.Length == 4)
            {
                username = args[2];
                password = args[3];
            }
            
        }

        public static void process(Uri uri, string username, string password, string response, string outputFile)
        {
            //Console.WriteLine(output);
            var results = JsonConvert.DeserializeObject<dynamic>(response);
            var metadataURI = results["@odata.context"].Value;
            var url = new Uri(metadataURI);
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
           
            HttpResponseValue httpResponseValue = Util.Get(metadataURI, username, password);
            if (httpResponseValue.getStatusCode() == 200)
            {
                StringReader stringReader = new StringReader(httpResponseValue.getResponseBody());
                XmlReader reader = XmlReader.Create(stringReader);
                IEdmModel model;
                IEnumerable<EdmError> errors;
                CsdlReader.TryParse(reader, out model, out errors);
                JObject swaggerDoc = new JObject()
                {
                    {"swagger", "2.0"},
                    {"info", new JObject()
                    {
                        {"title", "OData Service"},
                        {"description", "The OData Service at " + metadataURI},
                        {"version", version},
                        {"x-odata-version", "4.0"}
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

                foreach (var entitySet in model.EntityContainer.EntitySets())
                {
                    swaggerPaths.Add("/" + entitySet.Name + "*", CreateSwaggerPathForEntitySet(entitySet));
                    swaggerPaths.Add(GetPathForEntity(entitySet) + "*", CreateSwaggerPathForEntity(entitySet));
                }
                foreach (var operationImport in model.EntityContainer.OperationImports())
                {
                    swaggerPaths.Add(GetPathForOperationImport(operationImport) + "*", CreateSwaggerPathForOperationImport(operationImport));
                }

                foreach (var type in model.SchemaElements.OfType<IEdmStructuredType>())
                {
                    swaggeDefinitions.Add(type.FullTypeName(), CreateSwaggerDefinitionForStructureType(type));
                }

                foreach (var operation in model.SchemaElements.OfType<IEdmOperation>())
                {
                    // skip unbound operation
                    if (!operation.IsBound)
                    {
                        continue;
                    }
                    var boundParameter = operation.Parameters.First();
                    var boundType = boundParameter.Type.Definition;
                    // skip operation bound to non entity (or entity collection)
                    if (boundType.TypeKind == EdmTypeKind.Entity)
                    {
                        IEdmEntityType entityType = boundType as IEdmEntityType;
                        foreach (var entitySet in
                                model.EntityContainer.EntitySets().Where(es => es.EntityType().Equals(entityType)))
                        {
                            swaggerPaths.Add(GetPathForOperationOfEntity(operation, entitySet) + "*",
                                CreateSwaggerPathForOperationOfEntity(operation, entitySet));
                        }
                    }

                    else if (boundType.TypeKind == EdmTypeKind.Collection &&
                             (boundType as IEdmCollectionType).ElementType.Definition.TypeKind == EdmTypeKind.Entity)
                    {
                        IEdmEntityType entityType = boundType as IEdmEntityType;
                        foreach (
                            var entitySet in
                                model.EntityContainer.EntitySets().Where(es => es.EntityType().Equals(entityType)))
                        {
                            swaggerPaths.Add(GetPathForOperationOfEntitySet(operation, entitySet) + "*",
                                CreateSwaggerPathForOperationOfEntitySet(operation, entitySet));
                        }
                    }
                }

                swaggeDefinitions.Add("_Error", new JObject()
                {
                    {
                        "properties", new JObject()
                        {
                            {
                                "error", new JObject()
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
                            {
                                "code", new JObject()
                                {
                                    {"type", "string"}
                                }
                            },
                            {
                                "message", new JObject()
                                {
                                    {"type", "string"}
                                }
                            }
                        }
                    }
                });
                File.WriteAllText(outputFile, swaggerDoc.ToString());
            }   
        }

        public static void SetSwaggerType(JObject jObject, IEdmType edmType)
        {
            if (edmType.TypeKind == EdmTypeKind.Complex || edmType.TypeKind == EdmTypeKind.Entity)
            {
                jObject.Add("$ref", "#/definitions/" + edmType.FullTypeName());
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
                case EdmPrimitiveTypeKind.Date:
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
    }

}
