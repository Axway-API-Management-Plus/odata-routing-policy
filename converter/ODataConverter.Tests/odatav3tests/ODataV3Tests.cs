using System;
using Xunit;
using System.IO;

using Axway.apim;

namespace ODataConverterV3.Tests
{
    public class ODataV3Tests
    {
        [Fact]
        public void TestSAPV2ODataService()
        {
            string testFile = GetFullPathToFile("test-files/v2/sap-odata-service-v2.xml");

            var converter = new Axway.apim.V3ODataSwaggerConverter.AxwayODataV3Converter();
            string[] args = new string[2] { "file://"+ testFile, "swagger-out.json" };
            converter.Convert(args);
        }

        internal static string GetFullPathToFile(string pathRelativeUnitTestingFile)
        {
            string folderProjectLevel = GetPathToCurrentUnitTestProject();
            string final = System.IO.Path.Combine(folderProjectLevel, pathRelativeUnitTestingFile);
            return final;
        }

        private static string GetPathToCurrentUnitTestProject()
        {
            string pathAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string folderAssembly = System.IO.Path.GetDirectoryName(pathAssembly);
            if (folderAssembly.EndsWith("//") == false) folderAssembly = folderAssembly + "//";
            string folderProjectLevel = System.IO.Path.GetFullPath(folderAssembly + "..//..//..//");
            return folderProjectLevel;
        }
    }
}
