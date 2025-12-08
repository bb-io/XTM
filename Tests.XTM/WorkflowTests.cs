using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class WorkflowTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task MoveWorkflowsToNextStep_ReturnsResponse(InvocationContext context)
    {
        // Arrange
        var action = new WorkflowActions(context);
        var project = new ProjectRequest { ProjectId = "" };
        var jobs = new JobsRequest { JobIds = new List<string> { "" } };
        var mailing = new MailingRequest { Mailing = "DISABLED" };

        // Act
        var response = await action.MoveJobsToNextWorkflowStep(jobs, project, mailing);

        // Assert
        PrintResult(response);
    }
}
