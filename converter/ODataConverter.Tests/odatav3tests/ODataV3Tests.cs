using System;
using Xunit;

using Axway.apim;

namespace ODataConverterV3.Tests
{
    public class ODataV3Tests
    {
        [Fact]
        public void Test1()
        {
            var converter = new Axway.apim.V3ODataSwaggerConverter.AxwayODataV3Converter();
            string[] args = new string[2] { "http://api-env:8080/odata/metadata.xml", "swagger-out.json" };
            converter.Convert(args);
        }
    }
}
