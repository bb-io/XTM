using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Customers;
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

    [ContextDataSource, TestMethod]
    public async Task UpdateProject_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new ProjectActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2839755" };
        var input = new UpdateProjectRequest
        {
            TranslationMemoryPenaltyProfileId = "45314",
            TranslationMemoryTagIds = ["11491"],
            TerminologyTagIds = ["11493"],
            SubjectMatterId = "2775"
        };

        // Act
        var response = await action.UpdateProject(project, input);

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task CreateProject_ReturnsCreatedProject(InvocationContext context)
    {
        // Arrange
        var actions = new ProjectActions(context, FileManager);
        var request = new CreateProjectRequest
        {
            Name = "test with template",
            WorkflowId = "2741108",
            SourceLanguage = "en_CA",
            TargetLanguages = ["de_AT"],
            ProjectTemplateId = "2741120",
            AnalysisFinishedCallback = "123",
            InvoiceStatusChangedCallback = "123",
            JobFinishedCallback = "123",
            ProjectAcceptedCallback = "123",
            ProjectCreatedCallback = "123",
            ProjectFinishedCallback = "123",
            WorkflowTransitionCallback = "123",
        };
        var customerRequest = new CustomerRequest { CustomerId = "2725347" };

        // Act
        var result = await actions.CreateProject(customerRequest, request);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }
}