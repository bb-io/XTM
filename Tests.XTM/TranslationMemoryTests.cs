using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Files;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class TranslationMemoryTests : TestBase
    {
        [TestMethod]
        public async Task ImportTM_ReturndValue()
        {
            var action = new TranslationMemoryActions(InvocationContext,FileManager);

            var response = await action.ImportTMFile(new ImportTMRequest
            {
                CustomerId = "2028",
                File = new FileReference
                {
                    Name = "test.tmx"
                },
                ImportProjectName = "TestProject",
                SourceLanguage = "en_US",
                TargetLanguage = "fr_FR",
                TmStatus = "APPROVED",
                TmStatusImportType = "NONE",
                WhitespacesFormattingType = "KEEP_ALL_WHITESPACES",

            });
        }
    }
}
