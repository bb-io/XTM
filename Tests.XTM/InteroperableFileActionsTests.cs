using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.IO.Compression;
using System.Xml.Linq;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class InteroperableFileActionsTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod, TestCategory("Live"), Timeout(360000)]
    public async Task InteroperableFiles_LiveRoundTrip_MatchesFixtures(InvocationContext context)
    {
        var actions = new FileActions(context, FileManager);
        var interoperableActions = new InteroperableActions(context, FileManager);
        using var client = new XTMClient();
        var credentials = context.AuthenticationCredentialsProviders.ToArray();
        var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var inputDirectory = Path.Combine(projectDirectory, "TestFiles", "Input");
        var fixtureDirectory = Path.Combine(inputDirectory, "Interoperable");
        var outputDirectory = Path.Combine(projectDirectory, "TestFiles", "Output");
        var settings = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(fixtureDirectory, "Inputs", "cases.json")));
        var localFiles = new HashSet<string>();
        var runId = Guid.NewGuid().ToString("N");
        var runInputRelativeDirectory = Path.Combine("Interoperable", "Inputs", ".runs", runId);
        var runInputDirectory = Path.Combine(inputDirectory, runInputRelativeDirectory);
        ProjectRequest? project = null;
        Exception? testFailure = null;

        try
        {
            var projectInput = settings["Project"]!.ToObject<Dictionary<string, string>>()!;
            projectInput["name"] += $" {runId}";
            var created = await client.ExecuteXtmWithFormData<CreateProjectResponse>(
                "/projects", Method.Post, projectInput, credentials);
            project = new ProjectRequest { ProjectId = created.ProjectId };
            TestContext.WriteLine($"Created isolated live project {project.ProjectId}.");

            foreach (var fixture in settings["Cases"]!.Children<JObject>())
            {
                var format = fixture.Value<string>("Format")!;
                var sourceFile = fixture.Value<string>("SourceFile")!;
                var expectedPath = Path.Combine(fixtureDirectory, "ExpectedOutputs", fixture.Value<string>("ExpectedOutput")!);
                var expected = JObject.Parse(await File.ReadAllTextAsync(expectedPath));
                var translations = fixture["Translations"]!.ToObject<Dictionary<string, string>>()!;
                var fileNamePrefix = $"interoperable-{runId}-";
                var fileName = fileNamePrefix + sourceFile;

                var uploaded = await interoperableActions.UploadSelectedSourceXliff(project, new UploadSelectedSourceXliffRequest
                {
                    File = new FileReference
                    {
                        Name = Path.Combine("Interoperable", "Inputs", sourceFile),
                        ContentType = fixture.Value<string>("ContentType")!,
                    },
                    Name = fileName,
                    ExcludeSegmentStates = fixture["ExcludeSegmentStates"]!.ToObject<string[]>(),
                });
                var preparedPath = Path.Combine(outputDirectory, uploaded.File.Name);
                localFiles.Add(preparedPath);
                var sourceJobs = uploaded.Jobs;
                Assert.IsTrue(sourceJobs.All(x => x.FileName == uploaded.File.Name),
                    $"{format}: Upload returned jobs for another source file.");
                AssertMatchesFixture(expected["Upload"]!, JObject.FromObject(new
                {
                    FileName = uploaded.File.Name.Replace(fileNamePrefix, "", StringComparison.Ordinal),
                    uploaded.File.ContentType,
                    uploaded.Uploaded,
                    uploaded.SegmentsTotal,
                    uploaded.SegmentsExcluded,
                    uploaded.SegmentsLeft,
                    uploaded.ApproximateWordCount,
                    MatchingJobs = sourceJobs.Length,
                }), $"{format}: Upload");
                var jobId = sourceJobs.Single().JobId;
                TestContext.WriteLine($"Uploaded {format} source to project {project.ProjectId}, job {jobId}.");

                AssertMatchesFixture(expected["Prepared"]!, ReadXliffOutput(XDocument.Load(preparedPath), format == "xliff"),
                    $"{format}: Prepared");

                var analysisStatus = "";
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    var analysis = await client.ExecuteXtmWithJson<JObject>(
                        $"/projects/{project.ProjectId}/analysis", Method.Get, null, credentials);
                    analysisStatus = analysis.Value<string>("status");
                    if (analysisStatus == "FINISHED")
                        break;
                    await Task.Delay(1000);
                }
                Assert.AreEqual("FINISHED", analysisStatus, "XTM did not finish analyzing the source file.");

                var sourceDownload = await client.ExecuteXtmWithJson(
                    $"/projects/{project.ProjectId}/files/sources/download?jobIds={jobId}",
                    Method.Get, null, credentials);
                using (var sourceArchive = new ZipArchive(new MemoryStream(sourceDownload.RawBytes!)))
                {
                    var entry = sourceArchive.Entries.Single(x => !string.IsNullOrEmpty(x.Name));
                    using var sourceStream = entry.Open();
                    AssertMatchesFixture(expected["UploadedSource"]!, ReadXliffOutput(XDocument.Load(sourceStream), format == "xliff"),
                        $"{format}: UploadedSource");
                }

                var generated = await actions.GenerateFiles(project, new GenerateFileRequest
                {
                    FileType = "XLIFF",
                    JobIds = [jobId],
                });
                var generatedFileId = generated.Files.Single().FileId;
                var generatedStatus = "";
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    var status = await client.ExecuteXtmWithJson<JObject>(
                        $"/projects/{project.ProjectId}/files/{generatedFileId}/status?fileScope=JOB",
                        Method.Get, null, credentials);
                    generatedStatus = status.Value<string>("status");
                    Assert.AreNotEqual("ERROR", generatedStatus, status.Value<string>("message"));
                    if (generatedStatus == "FINISHED")
                        break;
                    await Task.Delay(1000);
                }
                AssertMatchesFixture(expected["Generated"]!, JObject.FromObject(new
                {
                    Status = generatedStatus,
                    FileCount = generated.Files.Count(),
                }), $"{format}: Generated");

                var exported = await actions.DownloadProjectFile(project, new DownloadProjectFileRequest
                {
                    FileId = generatedFileId,
                    FileScope = "JOB",
                });
                var exportedPath = Path.Combine(outputDirectory, exported.Content.Name);
                localFiles.Add(exportedPath);
                var translation = XDocument.Load(exportedPath);
                var analyzedUnits = translation.Descendants()
                    .Where(x => x.Name.LocalName == "trans-unit").ToArray();
                AssertMatchesFixture(expected["Analysis"]!, JObject.FromObject(new
                {
                    Status = analysisStatus,
                    Sources = analyzedUnits.Select(x => x.Elements().Single(e => e.Name.LocalName == "source").Value)
                        .OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                }), $"{format}: Analysis");
                foreach (var analyzedUnit in analyzedUnits)
                {
                    var analyzedSource = analyzedUnit.Elements().Single(x => x.Name.LocalName == "source");
                    var target = analyzedUnit.Elements().SingleOrDefault(x => x.Name.LocalName == "target");
                    if (target is null)
                    {
                        target = new XElement(analyzedSource.Name.Namespace + "target");
                        analyzedSource.AddAfterSelf(target);
                    }
                    target.Value = translations[analyzedSource.Value];
                    target.SetAttributeValue("state", fixture.Value<string>("TranslationState"));
                }
                var translatedFileName = $"translated-{format}.xlf";
                Directory.CreateDirectory(runInputDirectory);
                translation.Save(Path.Combine(runInputDirectory, translatedFileName));
                var translationUpload = await actions.UploadTranslationFile(project, new UploadTranslationFileRequest
                {
                    JobId = jobId,
                    FileType = "XLIFF",
                    Name = translatedFileName,
                    File = new FileReference
                    {
                        Name = Path.Combine(runInputRelativeDirectory, translatedFileName),
                        ContentType = "application/xliff+xml",
                    },
                }, new UploadTranslationFileEstimatesRequest());
                AssertMatchesFixture(expected["TranslationUpload"]!, JObject.FromObject(new { translationUpload.Status }),
                    $"{format}: TranslationUpload");

                var downloaded = await interoperableActions.DownloadTranslatedInteroperableFile(project,
                    new DownloadTranslatedInteroperableFileRequest { JobId = jobId });
                var downloadedPath = Path.Combine(outputDirectory, downloaded.File.Name);
                localFiles.Add(downloadedPath);
                AssertMatchesFixture(expected["Downloaded"]!, ReadXliffOutput(XDocument.Load(downloadedPath), format == "xliff"),
                    $"{format}: Downloaded");
                if (expected["Reconstructed"]!.Type != JTokenType.Null)
                {
                    using var downloadedStream = File.OpenRead(downloadedPath);
                    var reloaded = Transformation.Load(downloadedStream, downloaded.File.Name, downloaded.File.ContentType);
                    Assert.IsTrue(reloaded.Success, reloaded.Error);
                    var reconstructed = reloaded.Value!.Target();
                    Assert.IsTrue(reconstructed.Success, reconstructed.Error);
                    JObject nativeOutput;
                    if (format == "txt")
                    {
                        // Filters 1.2.16 routes text/plain ToStream() through PoCoder; inspect the plaintext content directly.
                        nativeOutput = JObject.FromObject(new { Text = reconstructed.Value!.GetPlaintext().Trim() });
                    }
                    else
                    {
                        using var reconstructedReader = new StreamReader(reconstructed.Value!.ToStream());
                        var html = new HtmlDocument();
                        html.LoadHtml(await reconstructedReader.ReadToEndAsync());
                        nativeOutput = JObject.FromObject(new
                        {
                            Paragraphs = html.DocumentNode.Descendants("p").Select(x => x.InnerText).ToArray(),
                            Description = html.DocumentNode.SelectSingleNode("//meta[@name='description']")
                                ?.GetAttributeValue("content", null),
                        });
                    }
                    AssertMatchesFixture(expected["Reconstructed"]!, nativeOutput, $"{format}: Reconstructed");
                }
                TestContext.WriteLine($"Verified {format} against {expectedPath}.");
            }
        }
        catch (Exception exception)
        {
            testFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                if (project is not null)
                {
                    for (var attempt = 0; ; attempt++)
                    {
                        try
                        {
                            await client.ExecuteXtmWithJson($"/projects/{project.ProjectId}?option=DELETE_WITH_TM",
                                Method.Delete, null, credentials);
                            break;
                        }
                        catch (PluginApplicationException exception) when (attempt < 59
                            && exception.Message.Contains("under analysis", StringComparison.OrdinalIgnoreCase))
                        {
                            await Task.Delay(1000);
                        }
                    }
                    TestContext.WriteLine($"Deleted isolated live project {project.ProjectId} and its test TM.");
                }
            }
            catch (Exception exception) when (testFailure is not null)
            {
                TestContext.WriteLine($"Cleanup failed for isolated project {project?.ProjectId}: {exception.Message}");
            }
            finally
            {
                foreach (var path in localFiles)
                    File.Delete(path);
                if (Directory.Exists(runInputDirectory))
                    Directory.Delete(runInputDirectory, recursive: true);
            }
        }
    }

    private static JObject ReadXliffOutput(XDocument document, bool includeUnitIds)
    {
        XNamespace xliffNamespace = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace markerNamespace = "https://blackbird.io/xliff/xtm-source-selection";
        return JObject.FromObject(new
        {
            Version = document.Root?.Attribute("version")?.Value,
            Namespace = document.Root?.Name.NamespaceName,
            SourceLanguage = document.Root?.Attribute("srcLang")?.Value,
            BlackbirdAttributeCount = document.Descendants().Attributes().Count(x => x.Name.Namespace == markerNamespace),
            Units = document.Descendants(xliffNamespace + "unit")
                .OrderBy(x => string.Concat(x.Descendants(xliffNamespace + "source").Select(s => s.Value)), StringComparer.Ordinal)
                .Select(unit => new
                {
                    Id = includeUnitIds ? unit.Attribute("id")?.Value : null,
                    Translate = unit.Attribute("translate")?.Value,
                    Excluded = unit.Attribute(markerNamespace + "excluded")?.Value,
                    Sources = unit.Descendants(xliffNamespace + "source").Select(x => x.Value).ToArray(),
                    Targets = unit.Descendants(xliffNamespace + "source")
                        .Select(x => x.Parent?.Element(xliffNamespace + "target")?.Value ?? "").ToArray(),
                }).ToArray(),
        });
    }

    private static void AssertMatchesFixture(JToken expected, JToken actual, string stage)
    {
        Assert.IsTrue(JToken.DeepEquals(expected, actual), $"{stage} differs from fixture.\nExpected:\n{expected}\nActual:\n{actual}");
    }
}
