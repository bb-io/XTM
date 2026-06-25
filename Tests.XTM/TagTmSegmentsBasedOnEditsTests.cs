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
            SourceLanguage = "en_GB",
            TargetLanguage = "es",
            CreatedByUserIds = ["111111", "111112"],
            //ApprovalStatus = "ALL",
            UntrustedUserIds = ["222222"],
            IncludeReverseMemory = false,
            DryRun = false,
            TagIds = ["45312"],
            ImportProjectName = $"Farfetch_Tag_By_Edits_Result_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
        });

        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ExportFileId));
        Assert.IsNotNull(result.FilteredTmxFile);
        Assert.AreEqual("DRY_RUN", result.ImportStatus);
        Assert.AreEqual(5, result.ExportedSegmentsScanned);
        Assert.AreEqual(3, result.SegmentsMatched);
        Assert.AreEqual(2, result.SegmentsSkipped);
        Assert.AreEqual(result.ExportedSegmentsScanned - result.SegmentsMatched, result.SegmentsSkipped);
    }
}
