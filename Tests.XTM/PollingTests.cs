using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Polling;
using Apps.XTM.Polling.Models.Memory;
using Blackbird.Applications.Sdk.Common.Polling;
using XTMTests.Base;

namespace Tests.XTM;

[TestClass]
public class PollingTests : TestBase
{
    [TestMethod]
    public async Task OnProjectsFinished_IsSuccess()
    {
        var polling = new PollingList(InvocationContext);

        //var oldDate = new DateTime(2025, 6, 4, 16, 29, 41, DateTimeKind.Utc);
        var oldDate = new DateTime(2025, 8, 13, 12, 23, 41, DateTimeKind.Utc);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory
            {
                LastInteractionDate = oldDate
            }
        };

        var result = await polling.OnProjectsFinished(request, new ProjectOptionalRequest { CustomerNameContains= "Webtoon" });

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        Console.WriteLine(json);
    }

    [TestMethod]
    public async Task OnProjectsUpdated_IsSuccess()
    {
        // Arrange
        var polling = new PollingList(InvocationContext);
        var oldDate = new DateTime(2025, 9, 10, 8, 5, 00, DateTimeKind.Utc);
        var request = new PollingEventRequest<DateMemory>
        {
            Memory = new DateMemory
            {
                LastInteractionDate = oldDate
            }
        };

        // Act
        var result = await polling.OnProjectsUpdated(request, new ProjectOptionalRequest { ProjectNameContains = "Test 1_1" });

        // Assert
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        Console.WriteLine(json);
    }
}
