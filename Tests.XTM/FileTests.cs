using Tests.XTM.Base;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class FileTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task GenerateFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "113735837" };
        var fileGenerate = new GenerateFileRequest { FileType = "XLIFF" };

        // Act
        var response = await action.GenerateFiles(project, fileGenerate);

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
}
