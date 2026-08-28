using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Transformations;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class FileActionsTests : TestBaseMultipleConnections
{
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

    [ContextDataSource(ConnectionTypes.Credentials), TestMethod, Timeout(90000)]
    public async Task AddMetadata_LiveProject_AppliesProvenancePriority(InvocationContext context)
    {
        var action = new FileActions(context, FileManager);
        var project = new ProjectRequest { ProjectId = "2854476" };
        var generated = await action.GenerateFiles(project, new GenerateFileRequest
        {
            FileType = "XLIFF",
            JobIds = ["2854490"],
        });
        Assert.IsNotEmpty(generated.Files);

        var modes = new[]
        {
            SegmentAttributionDataSourceHandler.All,
            SegmentAttributionDataSourceHandler.OnlyConfirmed,
            SegmentAttributionDataSourceHandler.OnlyChanged,
            SegmentAttributionDataSourceHandler.None,
        };

        foreach (var mode in modes)
        {
            FileResponse? result = null;
            for (var attempt = 0; attempt < 15 && result is null; attempt++)
            {
                try
                {
                    result = await action.AddMetadata(project, new AddMetadataRequest
                    {
                        File = new FileReference
                        {
                            Name = "exported-xliff-reviewed.xlf",
                            ContentType = "application/xliff+xml",
                        },
                        JobId = "2854490",
                        AttributeSegmentsToUser = mode,
                        WorkflowStep = mode == SegmentAttributionDataSourceHandler.All ? "translate1" : null,
                    });
                }
                catch (PluginMisconfigurationException ex) when (
                    ex.Message.Contains("Generate XLIFF files", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(2000);
                }
            }

            Assert.IsNotNull(result, $"Generated XLIFF did not become available for mode {mode}.");
            var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            var outputPath = Path.Combine(projectDirectory, "TestFiles", "Output", result.File.Name);
            await using var output = File.OpenRead(outputPath);
            var outputTransformation = Transformation.Load(output, result.File.Name).Value!;
            var units = outputTransformation.GetUnits().ToList();

            Assert.HasCount(5, units);
            if (mode is SegmentAttributionDataSourceHandler.All
                or SegmentAttributionDataSourceHandler.OnlyConfirmed)
            {
                Assert.IsTrue(units.All(x => x.Provenance.Translation.Person == "Blackbird (ID 92530)"));
                Assert.IsTrue(units.All(x => x.Provenance.Translation.Tool == "XTM"));
            }
            else if (mode == SegmentAttributionDataSourceHandler.OnlyChanged)
            {
                Assert.HasCount(3, units.Where(x => x.Provenance.Translation.Person == "Blackbird (ID 92530)"));
                Assert.AreEqual("XTM (exact match)", units[3].Provenance.Translation.Tool);
                Assert.AreEqual("XTM (mt suggestion)", units[4].Provenance.Translation.Tool);
            }
            else
            {
                Assert.IsTrue(units.All(x => x.Provenance.Translation.Person is null));
                Assert.HasCount(2, units.Where(x => x.Provenance.Translation.Tool == "XTM (exact match)"));
                Assert.HasCount(3, units.Where(x => x.Provenance.Translation.Tool == "XTM (mt suggestion)"));
            }
        }

        var groupedResult = await action.AddMetadata(project, new AddMetadataRequest
        {
            File = new FileReference
            {
                Name = "provenance-grouped-2.2.xlf",
                ContentType = "application/xliff+xml",
            },
            JobId = "2854490",
            AttributeSegmentsToUser = SegmentAttributionDataSourceHandler.OnlyChanged,
        });
        var groupedOutputPath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "TestFiles",
            "Output",
            groupedResult.File.Name);
        await using var groupedOutput = File.OpenRead(groupedOutputPath);
        var groupedUnits = Transformation.Load(groupedOutput, groupedResult.File.Name).Value!
            .GetUnits()
            .ToDictionary(x => x.Id!);
        Assert.AreEqual("Blackbird (ID 92530)", groupedUnits["t3"].Provenance.Translation.Person);
        Assert.IsNull(groupedUnits["t4"].Provenance.Translation.Person);
        Assert.AreEqual("XTM (exact match)", groupedUnits["t4"].Provenance.Translation.Tool);
        Assert.AreEqual("XTM (mt suggestion)", groupedUnits["t5"].Provenance.Translation.Tool);
        await groupedOutput.DisposeAsync();

        var reviewResult = await action.AddMetadata(project, new AddMetadataRequest
        {
            File = new FileReference
            {
                Name = "provenance-grouped-2.2.xlf",
                ContentType = "application/xliff+xml",
            },
            JobId = "2854490",
            AttributeSegmentsToUser = SegmentAttributionDataSourceHandler.OnlyChanged,
            ProvenanceType = ProvenanceTypeDataSourceHandler.Review,
        });
        var reviewOutputPath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "TestFiles",
            "Output",
            reviewResult.File.Name);
        await using var reviewOutput = File.OpenRead(reviewOutputPath);
        var reviewedUnits = Transformation.Load(reviewOutput, reviewResult.File.Name).Value!
            .GetUnits()
            .ToDictionary(x => x.Id!);
        Assert.AreEqual("Blackbird (ID 92530)", reviewedUnits["t3"].Provenance.Review.Person);
        Assert.AreEqual("XTM", reviewedUnits["t3"].Provenance.Review.Tool);
        Assert.AreEqual("Existing translator", reviewedUnits["t3"].Provenance.Translation.Tool);
        Assert.IsNull(reviewedUnits["t4"].Provenance.Review.Person);
        Assert.AreEqual("XTM (exact match)", reviewedUnits["t4"].Provenance.Review.Tool);

        var xliff1Result = await action.AddMetadata(project, new AddMetadataRequest
        {
            File = new FileReference
            {
                Name = "provenance-target-1.2.xlf",
                ContentType = "application/xliff+xml",
            },
            JobId = "2854490",
            AttributeSegmentsToUser = SegmentAttributionDataSourceHandler.None,
        });
        var xliff1OutputPath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "TestFiles",
            "Output",
            xliff1Result.File.Name);
        var xliff1Output = await File.ReadAllTextAsync(xliff1OutputPath);
        StringAssert.Contains(xliff1Output, "version=\"2.2\"");
    }
}
