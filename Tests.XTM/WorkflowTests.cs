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
    public async Task GetLatestAssignedUser_WithAssignedUser_ReturnsUser(InvocationContext context)
    {
        var action = new WorkflowActions(context);
        var project = new ProjectRequest { ProjectId = "2854476" };

        var response = await action.GetLatestAssignedUser(project, "2854490");

        Assert.IsNotNull(response);
        Assert.AreEqual("92530", response.UserId);
        Assert.AreEqual("Blackbird", response.UserName);
        PrintResult(response);
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task GetLatestAssignedUser_WithoutAssignedUser_ReturnsNullProperties(InvocationContext context)
    {
        var action = new WorkflowActions(context);
        var project = new ProjectRequest { ProjectId = "2854476" };

        var response = await action.GetLatestAssignedUser(project, "2854488");

        Assert.IsNotNull(response);
        Assert.IsNull(response.UserId);
        Assert.IsNull(response.UserName);
    }

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
