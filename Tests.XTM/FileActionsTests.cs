using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class FileActionsTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task GenerateFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "108698822" };
        var fileGenerate = new GenerateFileRequest { FileType = "XLIFF" };

        // Act
        var response = await actions.GenerateFiles(project, fileGenerate);

        // Assert
        TestContext.WriteLine($"Total files generated: {response.Files.Length}");
        foreach (var job in response.Files)
            TestContext.WriteLine($"{job.FileId} - {job.FileType}");
    }

    [ContextDataSource, TestMethod]
    public async Task UploadSourceFile_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var projectRequest = new ProjectRequest { ProjectId = "6883" };
        var fileRequest = new UploadSourceFileRequest
        {
            File = new FileReference { Name = "sample.txt", ContentType = "text/plain" },
            WorkflowId = "6290",
        };

        // Act
        var response = await actions.UploadSourceFile(projectRequest, fileRequest);

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task UploadSourceFile_MoreThan50_IsSuccess(InvocationContext context)
    {
        var actions = new FileActions(context, FileManager);
        var projectRequest = new ProjectRequest { ProjectId = "28090" };

        for (int i = 1; i <= 100; i++)
        {
            var fileRequest = new UploadSourceFileRequest
            {
                File = await FileManager.UploadTestFileAsync("sample.txt"),
                WorkflowId = "6430",
                Name = $"sample_{i:D3}.txt",
            };
            await actions.UploadSourceFile(
                projectRequest,
                fileRequest);

            await Task.Delay(4000);
        }
    }

    [ContextDataSource, TestMethod]
    public async Task DownloadSourceFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "28090" };
        var jobs = new JobsRequest
        {
            JobIds = [] // "30795"
        };

        // Act
        var response = await actions.DownloadSourceFiles(project, jobs);

        // Assert
        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task DownloadTranslatedFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "69721875" };
        var fileGenerate = new DownloadTranslationsRequest { };

        // Act
        var response = await action.DownloadTranslations(project, fileGenerate);

        // Assert
        int i = 0;
        foreach (var file in response.Files)
        {
            if (file.FileDescription is XtmProjectFileDescription projectFile)
            {
                TestContext.WriteLine($"[{i++}] File name: {projectFile.FileName}");
                TestContext.WriteLine($"    File ID: {projectFile.FileId}");
                TestContext.WriteLine($"    Job ID: {projectFile.JobId}");
                TestContext.WriteLine($"    Target Language: {projectFile.TargetLanguage}");
            }
        }
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task DownloadProjectFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "107731759" };
        var fileGenerate = new DownloadProjectFileRequest { FileScope = "JOB", FileId = "107965948" };

        // Act
        var response = await action.DownloadProjectFile(project, fileGenerate);

        // Assert
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task UploadTranslationFile_FromInteroperableXliff_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "107731759" };
        var fileGenerate = new UploadTranslationFileRequest
        {
            File =  new FileReference() { Name = "exported-xliff-reviewed.xlf", ContentType = "application/xliff+xml" },
            FileType = "XLIFF",
            JobId = "107965948"
        };
        var estimatesRequest = new UploadTranslationFileEstimatesRequest
        {
            MarkSegmentsUnderThresholdAsNotCompleted = true,
            LockSegmentsAboveThreshold = true,
        };

        // Act
        var response = await action.UploadTranslationFile(project, fileGenerate, estimatesRequest);

        // Assert
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task UploadTranslationFile_FromInteroperableXliff_OnlyLocking_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "107731759" };
        var fileGenerate = new UploadTranslationFileRequest
        {
            File = new FileReference() { Name = "exported-xliff-reviewed.xlf", ContentType = "application/xliff+xml" },
            FileType = "XLIFF",
            JobId = "107965948"
        };
        var estimatesRequest = new UploadTranslationFileEstimatesRequest
        {
            LockSegmentsAboveThreshold = true,
        };

        // Act
        var response = await action.UploadTranslationFile(project, fileGenerate, estimatesRequest);

        // Assert
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task UploadTranslationFile_FromInteroperableXliff_OnlyCompleted_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "107731759" };
        var fileGenerate = new UploadTranslationFileRequest
        {
            File = new FileReference() { Name = "exported-xliff-reviewed.xlf", ContentType = "application/xliff+xml" },
            FileType = "XLIFF",
            JobId = "107965948"
        };
        var estimatesRequest = new UploadTranslationFileEstimatesRequest
        {
            MarkSegmentsUnderThresholdAsNotCompleted = true,
        };

        // Act
        var response = await action.UploadTranslationFile(project, fileGenerate, estimatesRequest);

        // Assert
        Assert.IsNotNull(response);
    }
}
