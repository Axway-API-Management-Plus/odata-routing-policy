using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdataSwaggerConverter
{
    class HttpResponseValue
    {
        private string responseBody;
        private string contentType;
        private int statusCode;
        public HttpResponseValue(string responseBody, string contentType, int statusCode)
        {
            this.responseBody = responseBody;
            this.contentType = contentType;
            this.statusCode = statusCode;
        }

        public string getResponseBody()
        {
            return responseBody;
        }

        public string getContentType()
        {
            return contentType;
        }

        public int getStatusCode()
        {
            return statusCode;
        }


    }
}
