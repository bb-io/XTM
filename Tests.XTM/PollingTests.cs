using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Polling;
using Apps.XTM.Polling.Models.Memory;
using Blackbird.Applications.Sdk.Common.Polling;
using Newtonsoft.Json;
using XTMTests.Base;

namespace Tests.XTM;

[TestClass]
public class PollingTests : TestBase
{
    [TestMethod]
    public async Task OnProjectsFinished_IsSuccess()
    {
        // Arrange
        var polling = new PollingList(InvocationContext);
        var oldDate = new DateTime(2025, 09, 27, 12, 0, 0);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsFinished(request, new ProjectOptionalRequest { CustomerNameContains = "Track OMC" });

        // Assert
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task OnProjectsUpdated_IsSuccess()
    {
        // Arrange
        var polling = new PollingList(InvocationContext);
        var oldDate = new DateTime(2025, 9, 10, 8, 5, 00, DateTimeKind.Utc);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsUpdated(request, new ProjectOptionalRequest { ProjectNameContains = "Test 1_1" });

        // Assert
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task OnProjectsCreated_IsSuccess()
    {
        // Arrange
        var polling = new PollingList(InvocationContext);
        var oldDate = DateTime.UtcNow - TimeSpan.FromDays(30);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory { LastInteractionDate = oldDate }
        };

        // Act
        var result = await polling.OnProjectsCreated(request);

        // Assert
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Assert.IsNotNull(result);
    }
}
