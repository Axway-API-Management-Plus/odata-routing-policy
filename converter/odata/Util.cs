using System;
using System.IO;
using System.Text;
using System.Net;
using log4net;

namespace OdataSwaggerConverter
{
    class Util
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Util));

        public static HttpResponseValue Get(string uri, String username, String password) 
        {
            try
            {
                log.Debug("URL to call : " + uri.ToString());
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                if (username != null && password != null)
                {
                    string credential = username + ":" + password;
                    string authHeader = Convert.ToBase64String(Encoding.Default.GetBytes(credential));
                    request.Headers["Authorization"] = "Basic " + authHeader;
                }
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    int statusCode = (int) response.StatusCode; 
                    var contentType = response.Headers.Get("Content-Type");
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string responseBody = reader.ReadToEnd();
                        HttpResponseValue httpResponseValue = new HttpResponseValue(responseBody, contentType, statusCode);
                        return httpResponseValue;

                    }
                }
            }
            catch (WebException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}
