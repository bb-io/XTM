using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class WorkflowTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task MoveWorkflowsToNextStep_ReturnsResponse(InvocationContext context)
    {
        // Arrange
        var action = new WorkflowActions(context);
        var project = new ProjectRequest { ProjectId = "2840634" };
        var mailing = new MailingRequest { Mailing = "DISABLED" };
        var input = new MoveJobsToNextStepRequest 
        {
            JobIds = ["2840647"],
            CurrentWorkflowStep = "correct1"
        };

        // Act
        var response = await action.MoveJobsToNextWorkflowStep(project, mailing, input);

        // Assert
        PrintResult(response);
    }
}
