using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Glossaries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class GlossaryTests :TestBase
    {
        [TestMethod]
        public async Task ExportGlossaryTest()
        {
            var action = new GlossaryActions(InvocationContext, FileManager);
            var request = new GlossaryRequest
            {
                MainLanguage = "en_US",
                CustomerId = "644264",
                Languages = new List<string> { "it_IT" }
            };

            var response = await action.ExportGlossary(request);

            var json = JsonConvert.SerializeObject(response, Formatting.Indented);
            Console.WriteLine(json);
            Assert.IsNotNull(response.File);          
        }
    }
}
