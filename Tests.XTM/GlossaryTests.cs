using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Glossaries;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class GlossaryTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task ExportGlossary_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new GlossaryActions(context, FileManager);
        var request = new GlossaryRequest
        {
            MainLanguage = "en_US",
            CustomerId = "644264",
            Languages = new List<string> { "it_IT" }
        };

        // Act
        var response = await action.ExportGlossary(request);

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response.File);
    }
}
