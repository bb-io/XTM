using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class ProjectActionsTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task GetProject_ReturnsProjects(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);
        var input = new ProjectRequest { ProjectId = "66245898" };

        // Act
        var response = await action.GetProject(input);

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task GetProjectDetails_ReturnsProjectDetails(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);
        var input = new ProjectRequest { ProjectId = "2739500" };

        // Act
        var response = await action.GetProjectDetails(input);

        // Assert
        PrintResult(response);
    }

    [ContextDataSource, TestMethod]
    public async Task GetProjectCompletion_ReturnsProjectCompletionResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ProjectActions(context, FileManager);
        var input = new ProjectRequest { ProjectId = "2739558" };

        // Act
        var result = await actions.GetProjectCompletion(input);

        // Assert
        PrintResult(result);
    }

    [ContextDataSource, TestMethod]
    public async Task ListProject_ReturnsProjects(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);

        // Act
        var response = await action.ListProjects(new ListProjectsRequest() { CustomerIds = ["35961951"] });

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response.Projects);
    }

    [ContextDataSource, TestMethod]
    public async Task DownloadProjectFile_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var projectrequest = new ProjectRequest { ProjectId = "187237893" };
        var fileRequest = new DownloadProjectFileRequest { FileId = "187430508", FileScope = "PROJECT" };

        // Act
        var response = await action.DownloadProjectFile(projectrequest, fileRequest);

        // Assert
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task DownloadProjectFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var projectrequest = new ProjectRequest { ProjectId = "187363559" };
        var fileRequest = new DownloadAllProjectFilesRequest { FileScope = "PROJECT", FileType = "HTML_EXTENDED_TABLE" };

        // Act
        var response = await action.DownloadProjectFiles(projectrequest, fileRequest);

        // Assert
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task GetProjectStatus_ReturnsProjectStatus(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);

        // Act
        var response = await action.GetProjectStatus(new ProjectRequest { ProjectId = "2723018" });

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task GetProjectEstimates_ReturnsProjectEstimates(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);

        // Act
        var response = await action.GetProjectEstimates(new ProjectRequest { ProjectId = "2723018" });

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }
}