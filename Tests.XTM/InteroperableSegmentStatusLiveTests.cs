using System.Text;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.RestUtilities;
using Apps.XTM.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Moq;
using Newtonsoft.Json.Linq;
using RestSharp;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class InteroperableSegmentStatusLiveTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod, TestCategory("Live"), Timeout(900000)]
    public async Task StatusRoundtrip_CopiesStatesLocksSelectedSegmentsAndPreservesExcludedContent(InvocationContext context)
    {
        if (Environment.GetEnvironmentVariable("XTM_RUN_STATUS_ROUNDTRIP") != "1")
            Assert.Inconclusive("Set XTM_RUN_STATUS_ROUNDTRIP=1 to create and delete isolated XTM projects.");

        var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var settings = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(projectDirectory,
            "TestFiles", "Input", "Interoperable", "Inputs", "cases.json")));
        using var client = new XTMClient();
        var credentials = context.AuthenticationCredentialsProviders.ToArray();
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace native = "urn:oasis:names:tc:xliff:document:1.2";
        XNamespace xtm = "urn:xliff-xtm-extensions";
        XNamespace mapping = "https://blackbird.io/xliff/xtm-segment-mapping";

        foreach (var excludeCompleted in new[] { false, true })
        {
            var runId = Guid.NewGuid().ToString("N");
            var files = new Dictionary<string, byte[]>();
            var manager = new Mock<IFileManagementClient>(MockBehavior.Strict);
            manager.Setup(x => x.DownloadAsync(It.IsAny<FileReference>()))
                .Returns((FileReference file) => Task.FromResult<Stream>(new MemoryStream(files[file.Name])));
            manager.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(async (Stream stream, string contentType, string name) =>
                {
                    using var copy = new MemoryStream();
                    await stream.CopyToAsync(copy);
                    files[name] = copy.ToArray();
                    return new FileReference { Name = name, ContentType = contentType };
                });
            var actions = new FileActions(context, manager.Object);
            var interoperable = new InteroperableActions(context, manager.Object);
            ProjectRequest? project = null;
            Exception? failure = null;
            var artifactDirectory = Path.Combine(projectDirectory, "..", "artifacts", "status-roundtrip", runId);
            Directory.CreateDirectory(artifactDirectory);
            try
            {
                var projectInput = settings["Project"]!.ToObject<Dictionary<string, string>>()!;
                projectInput["name"] = $"Blackbird segment status roundtrip {runId}";
                var created = await client.ExecuteXtmWithFormData<CreateProjectResponse>(
                    "/projects", Method.Post, projectInput, credentials);
                project = new() { ProjectId = created.ProjectId };
                await File.WriteAllTextAsync(Path.Combine(artifactDirectory, "project-id.txt"), project.ProjectId);
                TestContext.WriteLine($"Created isolated project {project.ProjectId}; excluded-content case: {excludeCompleted}.");

                var sources = new[] { "Amber owl", "Violet fox", "Silver moth", "Copper ant", "Cobalt bee" }
                    .Select(text => $"{text} status roundtrip {runId}.").ToArray();
                var translations = sources.Select((source, index) => (source, text: $"Statusübersetzung {index + 1} {runId}."))
                    .ToDictionary(item => item.source, item => item.text);
                var html = $"<html lang=\"en-GB\"><head></head><body>{string.Join("", sources.Select((source, index) => $"<p data-blackbird-key=\"status-{index + 1}\">{source}</p>"))}</body></html>";
                var sourceName = $"status-{runId}.html";
                files[sourceName] = Encoding.UTF8.GetBytes(html);
                var sourceContentType = "text/html";
                if (excludeCompleted)
                {
                    var prepared = XliffSourceSelection.Prepare(files[sourceName], [], sourceName, sourceContentType, "en-GB");
                    var sourceDocument = XDocument.Parse(Encoding.UTF8.GetString(prepared.Content));
                    var sourceSegments = sourceDocument.Descendants(xliff + "segment").ToArray();
                    Assert.HasCount(5, sourceSegments);
                    foreach (var segment in sourceSegments.Take(2))
                    {
                        segment.SetAttributeValue("state", "final");
                        var target = segment.Element(xliff + "target");
                        if (target is null)
                        {
                            target = new XElement(xliff + "target");
                            segment.Add(target);
                        }
                        target.Value = translations[segment.Element(xliff + "source")!.Value];
                    }
                    sourceName += ".xlf";
                    sourceContentType = "application/xliff+xml";
                    files[sourceName] = Encoding.UTF8.GetBytes(sourceDocument.ToString());
                }

                var uploaded = await interoperable.UploadSelectedSourceXliff(project, new()
                {
                    File = new() { Name = sourceName, ContentType = sourceContentType },
                    ExcludeSegmentStates = ["final"],
                });
                Assert.AreEqual(5, uploaded.SegmentsTotal);
                Assert.AreEqual(excludeCompleted ? 2 : 0, uploaded.SegmentsExcluded);
                var jobId = uploaded.Jobs.Single().JobId;
                TestContext.WriteLine($"Uploaded five-segment source; job {jobId}, active segments {uploaded.SegmentsLeft}.");
                var analysisStatus = "";
                for (var attempt = 0; attempt < 90; attempt++)
                {
                    var analysis = await client.ExecuteXtmWithJson<JObject>($"/projects/{project.ProjectId}/analysis", Method.Get, null, credentials);
                    analysisStatus = analysis.Value<string>("status");
                    if (analysisStatus == "FINISHED") break;
                    Assert.AreNotEqual("ERROR", analysisStatus, analysis.ToString());
                    await Task.Delay(2000);
                }
                Assert.AreEqual("FINISHED", analysisStatus);

                var generated = await actions.GenerateFiles(project, new() { FileType = "XLIFF", JobIds = [jobId] });
                var generatedId = generated.Files.Single().FileId;
                var generationStatus = "";
                for (var attempt = 0; attempt < 90; attempt++)
                {
                    var status = await client.ExecuteXtmWithJson<JObject>(
                        $"/projects/{project.ProjectId}/files/{generatedId}/status?fileScope=JOB", Method.Get, null, credentials);
                    generationStatus = status.Value<string>("status");
                    if (generationStatus == "FINISHED") break;
                    Assert.IsFalse(generationStatus is "ERROR" or "WARNING", status.ToString());
                    await Task.Delay(2000);
                }
                Assert.AreEqual("FINISHED", generationStatus);
                var exported = await actions.DownloadProjectFile(project, new() { FileId = generatedId, FileScope = "JOB" });
                var offline = XDocument.Parse(Encoding.UTF8.GetString(files[exported.Content.Name]));
                var offlineUnits = offline.Descendants(native + "trans-unit").ToArray();
                Assert.HasCount(excludeCompleted ? 3 : 5, offlineUnits);
                foreach (var unit in offlineUnits)
                {
                    var target = unit.Element(native + "target")!;
                    target.Value = translations[unit.Element(native + "source")!.Value];
                    target.SetAttributeValue("state", "translated");
                }
                files["translated.xlf"] = Encoding.UTF8.GetBytes(offline.ToString());
                var translated = await actions.UploadTranslationFile(project,
                    new() { JobId = jobId, FileType = "XLIFF", File = new() { Name = "translated.xlf" }, SegmentStatusApproving = "NONE" }, new());
                Assert.AreEqual("FINISHED", translated.Status);
                TestContext.WriteLine("Uploaded translations; downloading full and mapped files.");

                var downloaded = await interoperable.DownloadTranslatedInteroperableFile(project,
                    new() { JobId = jobId, AttributeSegmentsToUser = "none", ProvenanceType = "translation" });
                var full = XDocument.Parse(Encoding.UTF8.GetString(files[downloaded.File.Name]));
                var fullSegments = full.Descendants(xliff + "segment").ToArray();
                Assert.HasCount(5, fullSegments);
                var mapped = XDocument.Parse(Encoding.UTF8.GetString(files[downloaded.TranslationFile.Name]));
                Assert.HasCount(excludeCompleted ? 3 : 5, mapped.Descendants(native + "trans-unit").ToArray());
                await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, "full-before-review.xlf"), files[downloaded.File.Name]);
                await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, "mapped-before-review.xlf"), files[downloaded.TranslationFile.Name]);
                foreach (var baselineUnit in mapped.Descendants(native + "trans-unit"))
                    TestContext.WriteLine($"Before status import {baselineUnit.Attribute("id")?.Value}: {(string?)baselineUnit.Element(native + "target")!.Attribute("state") ?? "(missing)"}.");

                string?[] states = excludeCompleted ? ["final", "final", "translated", "reviewed", "final"] : [null, "initial", "translated", "reviewed", "final"];
                for (var index = 0; index < fullSegments.Length; index++)
                    fullSegments[index].SetAttributeValue("state", states[index]);
                files["reviewed.xlf"] = Encoding.UTF8.GetBytes(full.ToString());
                var copied = await interoperable.CopySegmentStatusesToXtmXliff(new()
                {
                    TranslationFile = downloaded.TranslationFile,
                    TargetFile = new() { Name = "reviewed.xlf" },
                });
                var copiedDocument = XDocument.Parse(Encoding.UTF8.GetString(files[copied.File.Name]));
                var expectedStates = excludeCompleted ? new string?[] { "translated", "signed-off", "final" } : [null, "new", "translated", "signed-off", "final"];
                CollectionAssert.AreEqual(expectedStates, copiedDocument.Descendants(native + "trans-unit")
                    .Select(unit => (string?)unit.Element(native + "target")!.Attribute("state")).ToArray());
                await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, "mapped-after-review.xlf"), files[copied.File.Name]);
                var locked = await actions.UploadTranslationFile(project,
                    new() { JobId = jobId, FileType = "XLIFF", File = copied.File, SegmentStatusApproving = "ACCORDINGLY_TO_STATE" },
                    new() { LockSegmentByStates = ["reviewed", "final"] });
                Assert.AreEqual("FINISHED", locked.Status);
                TestContext.WriteLine("Uploaded copied statuses with reviewed/final locking; regenerating for verification.");

                var verified = await interoperable.DownloadTranslatedInteroperableFile(project,
                    new() { JobId = jobId, AttributeSegmentsToUser = "none", ProvenanceType = "translation" });
                await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, "verified-offline.xlf"), files[verified.TranslationFile.Name]);
                await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, "verified-full.xlf"), files[verified.File.Name]);
                var verifiedOffline = XDocument.Parse(Encoding.UTF8.GetString(files[verified.TranslationFile.Name]));
                var verifiedUnits = verifiedOffline.Descendants(native + "trans-unit").ToArray();
                Assert.HasCount(excludeCompleted ? 3 : 5, verifiedUnits);
                foreach (var unit in verifiedUnits)
                {
                    var source = unit.Element(native + "source")!.Value;
                    var index = Array.IndexOf(sources, source);
                    Assert.AreEqual(translations[source], unit.Element(native + "target")!.Value);
                    var isLocked = (string?)unit.Attribute(xtm + "locked") == "yes" || (string?)unit.Attribute("translate") == "no";
                    Assert.AreEqual(index >= 3, isLocked, $"Unexpected lock for segment {index + 1}: {unit}");
                    var state = (string?)unit.Element(native + "target")!.Attribute("state");
                    TestContext.WriteLine($"Verified segment {index + 1}: state={state ?? "(missing)"}, locked={isLocked}, mapped unit={unit.Attribute(mapping + "unit-id")?.Value}.");
                    Assert.AreEqual(index >= 2 ? "signed-off" : "translated", state,
                        "XTM approves translated, signed-off, and final imports; missing/new states retain unapproved translations.");
                }
                var verifiedFull = XDocument.Parse(Encoding.UTF8.GetString(files[verified.File.Name]));
                Assert.HasCount(5, verifiedFull.Descendants(xliff + "segment").ToArray());
                if (excludeCompleted)
                {
                    var excluded = verifiedFull.Descendants(xliff + "segment").Take(2).ToArray();
                    Assert.IsTrue(excluded.All(segment => (string?)segment.Attribute("state") == "final"));
                    CollectionAssert.AreEqual(sources.Take(2).Select(source => translations[source]).ToArray(),
                        excluded.Select(segment => segment.Element(xliff + "target")!.Value).ToArray());
                    Assert.IsFalse(verifiedUnits.Any(unit => sources.Take(2).Contains(unit.Element(native + "source")!.Value)));
                }
                TestContext.WriteLine($"Verified status and lock roundtrip. Artifacts: {artifactDirectory}");
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                if (project is not null)
                {
                    try
                    {
                        for (var attempt = 0; ; attempt++)
                        {
                            try
                            {
                                await client.ExecuteXtmWithJson($"/projects/{project.ProjectId}?option=DELETE_WITH_TM", Method.Delete, null, credentials);
                                break;
                            }
                            catch (PluginApplicationException exception) when (attempt < 59
                                && exception.Message.Contains("under analysis", StringComparison.OrdinalIgnoreCase))
                            {
                                await Task.Delay(1000);
                            }
                        }
                        await File.WriteAllTextAsync(Path.Combine(artifactDirectory, "cleanup.txt"), $"Deleted project {project.ProjectId} and its test TM.");
                        TestContext.WriteLine($"Deleted isolated project {project.ProjectId} and its test TM.");
                    }
                    catch (Exception cleanupFailure)
                    {
                        TestContext.WriteLine($"Cleanup failed for isolated project {project.ProjectId}: {cleanupFailure.Message}");
                        if (failure is not null)
                            throw new AggregateException("Live test and project cleanup both failed.", failure, cleanupFailure);
                        throw;
                    }
                }
            }
        }
    }
}
