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
using Newtonsoft.Json.Linq;
using RestSharp;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class InteroperableFileActionsTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod, TestCategory("Live"), Timeout(360000)]
    public async Task InteroperableFiles_LiveRoundTrip_TranslatesSupportedFilesAndRestoresOnlyAddedExclusions(
        InvocationContext context)
    {
        var actions = new FileActions(context, FileManager);
        using var client = new XTMClient();
        var credentials = context.AuthenticationCredentialsProviders.ToArray();
        var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var inputDirectory = Path.Combine(projectDirectory, "TestFiles", "Input");
        var outputDirectory = Path.Combine(projectDirectory, "TestFiles", "Output");
        var localFiles = new HashSet<string>();
        var runId = Guid.NewGuid().ToString("N");
        ProjectRequest? project = null;
        Exception? testFailure = null;
        XNamespace xliffNamespace = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace markerNamespace = "https://blackbird.io/xliff/xtm-source-selection";
        const string translatedText = "Dieser Satz wurde live übersetzt.";

        try
        {
            var created = await client.ExecuteXtmWithFormData<CreateProjectResponse>(
                "/projects", Method.Post, new Dictionary<string, string>
                {
                    ["name"] = $"Blackbird interoperable roundtrip test {runId}",
                    ["customerId"] = "2725347",
                    ["workflowId"] = "5896",
                    ["sourceLanguage"] = "en_GB",
                    ["targetLanguages"] = "de_DE",
                }, credentials);
            project = new ProjectRequest { ProjectId = created.ProjectId };
            TestContext.WriteLine($"Created isolated live project {project.ProjectId}.");

            foreach (var format in new[] { "xliff", "html", "txt" })
            {
                var expectedEditableSegments = format == "html" ? 2 : 1;
                var sourceText = $"Translate this {format} probe sentence.";
                var fileName = $"interoperable-{runId}-{format}.{format}";
                var source = format == "xliff"
                    ? $$"""
                      <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en-GB" trgLang="de-DE">
                        <file id="f1" original="probe.txt">
                          <unit id="original" translate="no"><segment id="original-segment"><source>Original locked probe</source><target>Ursprünglich gesperrt</target></segment></unit>
                          <unit id="completed"><segment id="completed-segment" state="final"><source>Completed probe</source><target>Abgeschlossene Probe</target></segment></unit>
                          <unit id="editable"><segment id="editable-segment" state="initial"><source>{{sourceText}}</source><target /></segment></unit>
                        </file>
                      </xliff>
                      """
                    : format == "html"
                        ? $"<!DOCTYPE html><html lang=\"en-GB\"><head><meta name=\"description\" content=\"Probe description\" /></head><body><p>{sourceText}</p></body></html>"
                        : sourceText;
                var sourcePath = Path.Combine(inputDirectory, fileName);
                localFiles.Add(sourcePath);
                await File.WriteAllTextAsync(sourcePath, source, new UTF8Encoding(format == "html"));

                var uploaded = await actions.UploadSelectedSourceXliff(project, new UploadSelectedSourceXliffRequest
                {
                    File = new FileReference
                    {
                        Name = fileName,
                        ContentType = format == "xliff" ? "application/xliff+xml"
                            : format == "html" ? "text/html" : "text/plain",
                    },
                    ExcludeSegmentStates = ["final"],
                });
                var preparedPath = Path.Combine(outputDirectory, uploaded.File.Name);
                localFiles.Add(preparedPath);
                Assert.IsTrue(uploaded.Uploaded);
                Assert.AreEqual(expectedEditableSegments, uploaded.SegmentsLeft);
                Assert.AreEqual(format == "xliff" ? 2 : 0, uploaded.SegmentsExcluded);
                var sourceJobs = uploaded.Jobs.Where(x => x.FileName == uploaded.File.Name).ToArray();
                Assert.HasCount(1, sourceJobs);
                var jobId = sourceJobs.Single().JobId;
                TestContext.WriteLine($"Uploaded {format} source to project {project.ProjectId}, job {jobId}.");

                var prepared = XDocument.Load(preparedPath);
                Assert.AreEqual("2.1", prepared.Root?.Attribute("version")?.Value);
                Assert.AreEqual(xliffNamespace, prepared.Root?.Name.Namespace);
                Assert.AreEqual("en-GB", prepared.Root?.Attribute("srcLang")?.Value);
                if (format != "xliff")
                    Assert.AreEqual(fileName + ".xlf", uploaded.File.Name);
                if (format == "xliff")
                {
                    var preparedUnits = prepared.Descendants(xliffNamespace + "unit")
                        .ToDictionary(x => x.Attribute("id")!.Value);
                    Assert.AreEqual("no", preparedUnits["original"].Attribute("translate")?.Value);
                    Assert.IsNull(preparedUnits["original"].Attribute(markerNamespace + "excluded"));
                    Assert.AreEqual("no", preparedUnits["completed"].Attribute("translate")?.Value);
                    Assert.IsNotNull(preparedUnits["completed"].Attribute(markerNamespace + "excluded"));
                }

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
                    var uploadedDocument = XDocument.Load(sourceStream);
                    Assert.AreEqual("2.1", uploadedDocument.Root?.Attribute("version")?.Value);
                    Assert.AreEqual(prepared.Descendants(xliffNamespace + "unit").Count(),
                        uploadedDocument.Descendants(xliffNamespace + "unit").Count());
                }

                var generated = await actions.GenerateFiles(project, new GenerateFileRequest
                {
                    FileType = "XLIFF",
                    JobIds = [jobId],
                });
                Assert.HasCount(1, generated.Files);
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
                Assert.AreEqual("FINISHED", generatedStatus, "XTM did not finish generating the analysis XLIFF.");

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
                Assert.HasCount(expectedEditableSegments, analyzedUnits, "XTM should analyze only editable source segments.");
                Assert.HasCount(1, analyzedUnits.Where(x => x.Elements()
                    .Any(element => element.Name.LocalName == "source" && element.Value == sourceText)));
                foreach (var analyzedUnit in analyzedUnits)
                {
                    var analyzedSource = analyzedUnit.Elements().Single(x => x.Name.LocalName == "source");
                    Assert.IsTrue(analyzedSource.Value == sourceText
                        || (format == "html" && analyzedSource.Value == "Probe description"));
                    var target = analyzedUnit.Elements().SingleOrDefault(x => x.Name.LocalName == "target");
                    if (target is null)
                    {
                        target = new XElement(analyzedSource.Name.Namespace + "target");
                        analyzedSource.AddAfterSelf(target);
                    }
                    target.Value = analyzedSource.Value == sourceText ? translatedText : analyzedSource.Value;
                    target.SetAttributeValue("state", "translated");
                }
                var translatedFileName = $"translated-{runId}-{format}.xlf";
                var translatedInputPath = Path.Combine(inputDirectory, translatedFileName);
                localFiles.Add(translatedInputPath);
                translation.Save(translatedInputPath);
                var translationUpload = await actions.UploadTranslationFile(project, new UploadTranslationFileRequest
                {
                    JobId = jobId,
                    FileType = "XLIFF",
                    File = new FileReference { Name = translatedFileName, ContentType = "application/xliff+xml" },
                }, new UploadTranslationFileEstimatesRequest());
                Assert.AreEqual("FINISHED", translationUpload.Status);

                var downloaded = await actions.DownloadTranslatedInteroperableFile(project,
                    new DownloadTranslatedInteroperableFileRequest { JobId = jobId });
                var downloadedPath = Path.Combine(outputDirectory, downloaded.File.Name);
                localFiles.Add(downloadedPath);
                var result = XDocument.Load(downloadedPath);
                Assert.AreEqual("2.1", result.Root?.Attribute("version")?.Value);
                Assert.IsTrue(result.Descendants(xliffNamespace + "target").Any(x => x.Value == translatedText));
                Assert.IsFalse(result.Root!.DescendantsAndSelf().Attributes().Any(x => x.Name.Namespace == markerNamespace));
                if (format == "xliff")
                {
                    var units = result.Descendants(xliffNamespace + "unit")
                        .ToDictionary(x => x.Attribute("id")!.Value);
                    Assert.HasCount(3, units);
                    Assert.AreEqual("no", units["original"].Attribute("translate")?.Value);
                    Assert.AreEqual("Ursprünglich gesperrt", units["original"].Descendants(xliffNamespace + "target").Single().Value);
                    Assert.IsNull(units["completed"].Attribute("translate"));
                    Assert.AreEqual("Abgeschlossene Probe", units["completed"].Descendants(xliffNamespace + "target").Single().Value);
                    Assert.AreEqual(translatedText, units["editable"].Descendants(xliffNamespace + "target").Single().Value);
                }
                else
                {
                    using var downloadedStream = File.OpenRead(downloadedPath);
                    var reloaded = Transformation.Load(downloadedStream, downloaded.File.Name, downloaded.File.ContentType);
                    Assert.IsTrue(reloaded.Success, reloaded.Error);
                    var reconstructed = reloaded.Value!.Target();
                    Assert.IsTrue(reconstructed.Success, reconstructed.Error);
                    if (format == "txt")
                    {
                        // Filters 1.2.16 routes text/plain ToStream() through PoCoder; inspect the plaintext content directly.
                        StringAssert.Contains(reconstructed.Value!.GetPlaintext(), translatedText);
                    }
                    else
                    {
                        using var reconstructedReader = new StreamReader(reconstructed.Value!.ToStream());
                        var nativeTarget = await reconstructedReader.ReadToEndAsync();
                        StringAssert.Contains(nativeTarget, translatedText);
                        StringAssert.Contains(nativeTarget, "name=\"description\" content=\"Probe description\"");
                    }
                }
                TestContext.WriteLine($"Verified {format}: XLIFF 2.1 upload, {expectedEditableSegments} analyzed segments, live German translation, exclusions restored.");
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
            }
        }
    }
}
