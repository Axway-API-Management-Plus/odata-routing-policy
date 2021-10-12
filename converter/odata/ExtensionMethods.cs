
using Newtonsoft.Json.Linq;
using Microsoft.Data.Edm;



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
                        {"$ref", refType}
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
                {"schema", schema}
            });
            return responses;
        }

        public static JObject Response(this JObject responses, string name, string description, Microsoft.OData.Edm.IEdmType type)
        {
            var schema = new JObject();
            OData4.SetSwaggerType(schema, type);
            responses.Add(name, new JObject()
            {
                {"description", description},
                {"schema", schema}
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
          Microsoft.OData.Edm.IEdmType type)
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
                var schema = new JObject();
                Program.SetSwaggerType(schema, type);
                parameter.Add("schema", schema);
            }
            if (required != null)
            {
                parameter.Add("required", required);
            }
            parameters.Add(parameter);
            return parameters;
        }

        public static JArray Parameter(this JArray parameters, string name, string kind, string description,
            Microsoft.OData.Edm.IEdmType type, bool? required)
        {
            var parameter = new JObject()
            {
                {"name", name},
                {"in", kind},
                {"description", description},
            };

            if (kind != "body")
            {
                OData4.SetSwaggerType(parameter, type);
            }
            else
            {
                var schema = new JObject();
                OData4.SetSwaggerType(schema, type);
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
                {"description", description},
                {
                    "schema", new JObject()
                    {
                        {"$ref", refType}
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
}
