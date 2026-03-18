using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
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
        var mailing = new MailingRequest { Mailing = "DISABLED" };
        var input = new MoveJobsToNextStepRequest 
        {
            JobIds = [],
            CurrentWorkflowStep = ""
        };

        // Act
        var response = await action.MoveJobsToNextWorkflowStep(project, mailing, input);

        // Assert
        PrintResult(response);
    }
}
