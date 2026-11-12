using Apps.XTM.Models.Request.Files;

namespace Tests.XTM;

[TestClass]
public class UploadSourceFileMatchRequestTests
{
    [TestMethod]
    public void GetMatchType_ReplaceExistingFile_ReturnsMatchNames()
    {
        var request = new UploadSourceFileMatchRequest { ReplaceExistingFile = true };

        Assert.AreEqual("MATCH_NAMES", request.GetMatchType());
    }

    [TestMethod]
    public void GetMatchType_DoNotReplaceExistingFile_ReturnsNoMatch()
    {
        var request = new UploadSourceFileMatchRequest { ReplaceExistingFile = false };

        Assert.AreEqual("NO_MATCH", request.GetMatchType());
    }

    [TestMethod]
    public void GetMatchType_ReplaceExistingFileNotSet_ReturnsNullToKeepApiDefault()
    {
        var request = new UploadSourceFileMatchRequest();

        Assert.IsNull(request.GetMatchType());
    }
}
