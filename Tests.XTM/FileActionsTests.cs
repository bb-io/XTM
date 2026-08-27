using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.XTM.Base;
using System.Xml.Linq;

namespace Tests.XTM;

[TestClass]
public class FileActionsTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_MachineTranslationSandbox_WritesTranslationTool(InvocationContext context)
    {
        var testsDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var fileName = "live-machine-translation.XLF";
        File.Copy(
            Path.Combine(testsDirectory, "TestFiles/Input", "provenance-machine-translation.xlf"),
            Path.Combine(testsDirectory, "TestFiles/Input", fileName),
            true);
        var action = new FileActions(context, FileManager);

        var result = await action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854432" }, new AddLatestProvenanceDataRequest
        {
            File = new FileReference { Name = fileName, ContentType = "application/xliff+xml" }
        });

        Assert.AreEqual(fileName, result.File.Name);
        var output = XDocument.Load(Path.Combine(testsDirectory, "TestFiles/Output", fileName));
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.2";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var file = output.Root!.Element(xliff + "file")!;
        Assert.AreEqual("XTM machine translation", file.Attribute(its + "tool")?.Value);
        Assert.IsNull(file.Attribute(its + "person"));
        File.Delete(Path.Combine(testsDirectory, "TestFiles/Input", fileName));
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_HumanReviewSandbox_WritesTranslationAndRevision(InvocationContext context)
    {
        var testsDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var fileName = "live-human-review.xlf";
        File.Copy(
            Path.Combine(testsDirectory, "TestFiles/Input", "provenance-human-review.xlf"),
            Path.Combine(testsDirectory, "TestFiles/Input", fileName),
            true);
        var action = new FileActions(context, FileManager);

        var result = await action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854312" }, new AddLatestProvenanceDataRequest
        {
            File = new FileReference { Name = fileName, ContentType = "application/xliff+xml" },
            JobId = "2854323"
        });

        Assert.AreEqual(fileName, result.File.Name);
        var output = XDocument.Load(Path.Combine(testsDirectory, "TestFiles/Output", fileName));
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.2";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var file = output.Root!.Element(xliff + "file")!;
        Assert.AreEqual("Blackbird Automation", file.Attribute(its + "person")?.Value);
        Assert.AreEqual("XTM", file.Attribute(its + "tool")?.Value);
        Assert.AreEqual("Blackbird Automation", file.Attribute(its + "revPerson")?.Value);
        Assert.AreEqual("XTM", file.Attribute(its + "revTool")?.Value);
        File.Delete(Path.Combine(testsDirectory, "TestFiles/Input", fileName));
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_Xliff2_WritesFileLevelProvenance(InvocationContext context)
    {
        var action = new FileActions(context, FileManager);
        var testsDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var fileName = "live-existing-provenance.xliff";
        var input = File.ReadAllText(Path.Combine(testsDirectory, "TestFiles/Input/sample-interoperable.xliff"))
            .Replace("<file id=\"f1\">", "<file id=\"f1\" its:tool=\"Existing translation tool\">");
        File.WriteAllText(Path.Combine(testsDirectory, "TestFiles/Input", fileName), input);

        await action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854312" }, new AddLatestProvenanceDataRequest
        {
            File = new FileReference { Name = fileName, ContentType = "application/xliff+xml" },
            JobId = "2854323",
            Placement = "Revision"
        });

        var output = XDocument.Load(Path.Combine(testsDirectory, "TestFiles/Output", fileName));
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.2";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var file = output.Root!.Element(xliff + "file")!;
        Assert.AreEqual("Existing translation tool", file.Attribute(its + "tool")?.Value);
        Assert.AreEqual("Blackbird Automation", file.Attribute(its + "revPerson")?.Value);
        Assert.AreEqual("XTM", file.Attribute(its + "revTool")?.Value);
        Assert.IsFalse(file.Descendants(xliff + "unit").Any(unit => unit.Attribute(its + "revTool")?.Value == "XTM"));
        File.Delete(Path.Combine(testsDirectory, "TestFiles/Input", fileName));
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_RejectsJobFromAnotherProject(InvocationContext context)
    {
        var action = new FileActions(context, FileManager);

        var exception = await Assert.ThrowsExactlyAsync<Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException>(() =>
            action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854312" }, new AddLatestProvenanceDataRequest
            {
                File = new FileReference { Name = "sample-interoperable.xliff", ContentType = "application/xliff+xml" },
                JobId = "2854442"
            }));

        StringAssert.Contains(exception.Message, "does not belong to project");
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_RejectsNonXliffExtension(InvocationContext context)
    {
        var action = new FileActions(context, FileManager);

        var exception = await Assert.ThrowsExactlyAsync<Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException>(() =>
            action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854312" }, new AddLatestProvenanceDataRequest
            {
                File = new FileReference { Name = "sample.txt", ContentType = "text/plain" }
            }));

        StringAssert.Contains(exception.Message, "Only XLIFF files");
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task AddLatestProvenanceData_RejectsMalformedXliff(InvocationContext context)
    {
        var testsDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var fileName = "live-malformed.xlf";
        File.WriteAllText(Path.Combine(testsDirectory, "TestFiles/Input", fileName), "<xliff version=\"1.2\"><broken>");
        var action = new FileActions(context, FileManager);

        var exception = await Assert.ThrowsExactlyAsync<Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException>(() =>
            action.AddLatestProvenanceData(new ProjectRequest { ProjectId = "2854312" }, new AddLatestProvenanceDataRequest
            {
                File = new FileReference { Name = fileName, ContentType = "application/xliff+xml" }
            }));

        StringAssert.Contains(exception.Message, "valid XLIFF");
        File.Delete(Path.Combine(testsDirectory, "TestFiles/Input", fileName));
    }

    [ContextDataSource, TestMethod]
    public async Task GenerateFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2741165" };
        var fileGenerate = new GenerateFileRequest { FileType = "XLIFF" };

        // Act
        var response = await actions.GenerateFiles(project, fileGenerate);

        // Assert
        TestContext.WriteLine($"Total files generated: {response.Files.Length}");
        foreach (var job in response.Files)
            TestContext.WriteLine($"{job.FileId} - {job.FileType}");
    }

    [ContextDataSource, TestMethod]
    public async Task GenerateFiles_FilterByActiveStatus_WontFailOnNoMatch(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2741165" };
        var fileGenerate = new GenerateFileRequest
        {
            FileType = "XLIFF",
            ActiveWorkflowSteps = ["translation1", "correct1"],
        };

        // Act
        var response = await actions.GenerateFiles(project, fileGenerate);

        // Assert
        TestContext.WriteLine($"Total files generated: {response.Files.Length}");
        foreach (var job in response.Files)
            TestContext.WriteLine($"{job.FileId} - {job.FileType}");
    }

    [ContextDataSource, TestMethod]
    public async Task GenerateFiles_FilterByActiveStatus_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2741165" };
        var fileGenerate = new GenerateFileRequest
        {
            FileType = "XLIFF",
            ActiveWorkflowSteps = ["automated post-editing1"],
        };

        // Act
        var response = await actions.GenerateFiles(project, fileGenerate);

        // Assert
        TestContext.WriteLine($"Total files generated: {response.Files.Length}");
        foreach (var job in response.Files)
            TestContext.WriteLine($"{job.FileId} - {job.FileType}");
    }

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task UploadSourceFile_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new FileActions(context, FileManager);
        var projectRequest = new ProjectRequest { ProjectId = "2844599" };
        var fileRequest = new UploadSourceFileRequest
        {
            File = new FileReference { Name = "sample.txt", ContentType = "text/plain" },
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

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod]
    public async Task DownloadReferenceFiles_IsSuccess(InvocationContext context)
    {
        // Arrange
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2844729" };

        // Act
        var result = await action.DownloadReferenceFiles(project);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.RawFiles);
    }
}
