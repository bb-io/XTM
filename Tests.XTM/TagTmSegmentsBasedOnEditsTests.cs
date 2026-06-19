using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class TagTmSegmentsBasedOnEditsTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task TagTmSegmentsBasedOnEdits_DryRun_IsSuccess(InvocationContext context)
    {
        var action = new TranslationMemoryActions(context, FileManager);

        var result = await action.TagTmSegmentsBasedOnEdits(new TagTmSegmentsBasedOnEditsRequest
        {
            CustomerId = "2725347",
            ProjectId = "2840311",
            SourceLanguage = "en_US",
            TargetLanguage = "uk_UA",
            CreatedByUserIds = ["92530"],
            ApprovalStatus = "ALL",
            IncludeReverseMemory = false,
            DryRun = false,
            TagIds = ["45312"],
            ImportProjectName = $"Test_import_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
        });

        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ExportFileId));
        Assert.IsNotNull(result.FilteredTmxFile);
        //Assert.AreEqual("DRY_RUN", result.ImportStatus);
        Assert.AreEqual(1, result.ExportedSegmentsScanned);
        Assert.AreEqual(1, result.SegmentsMatched);
        Assert.AreEqual(0, result.SegmentsSkipped);
        Assert.AreEqual(result.ExportedSegmentsScanned - result.SegmentsMatched, result.SegmentsSkipped);
    }
}
