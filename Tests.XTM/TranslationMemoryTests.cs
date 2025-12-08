using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class TranslationMemoryTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task ImportTM_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new TranslationMemoryActions(context, FileManager);

        // Act
        await action.ImportTMFile(new ImportTMRequest
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
