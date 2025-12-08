using Tests.XTM.Base;
using Apps.XTM.Polling;
using Apps.XTM.Constants;
using Apps.XTM.Polling.Models.Memory;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class PollingTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task OnProjectsFinished_IsSuccess(InvocationContext context)
    {
        // Arrange
        var polling = new PollingList(context);
        var oldDate = new DateTime(2025, 09, 27, 12, 0, 0);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsFinished(request, new ProjectOptionalRequest { CustomerNameContains = "Track OMC" });

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [ContextDataSource, TestMethod]
    public async Task OnProjectsUpdated_IsSuccess(InvocationContext context)
    {
        // Arrange
        var polling = new PollingList(context);
        var oldDate = new DateTime(2025, 9, 10, 8, 5, 00, DateTimeKind.Utc);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsUpdated(request, new ProjectOptionalRequest { ProjectNameContains = "Test 1_1" });

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [ContextDataSource, TestMethod]
    public async Task OnProjectsCreated_IsSuccess(InvocationContext context)
    {
        // Arrange
        var polling = new PollingList(context);
        var oldDate = DateTime.UtcNow - TimeSpan.FromDays(30);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsCreated(request);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task OnProjectAnalysisFinished_IsSuccess(InvocationContext context)
    {
        // Arrange
        var polling = new PollingList(context);
        var request = new PollingEventRequest<AnalysisStatusMemory>
        {
            Memory = new AnalysisStatusMemory { ProjectID = "201382646", Status = "started" }
        };
        var input = new ProjectRequest { ProjectId = "201382646" };

        // Act
        var result = await polling.OnProjectAnalysisFinished(request, input);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }
}
