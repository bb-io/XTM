using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Moq;

namespace Tests.XTM;

[TestClass]
public class InteroperableDownloadTests
{
    [TestMethod]
    [DataRow("ready", null)]
    [DataRow("target-pending", null)]
    [DataRow("xliff-pending", null)]
    [DataRow("missing-target", "Expected one generated TARGET file")]
    [DataRow("duplicate-xliff", "Expected one generated XLIFF file")]
    [DataRow("empty-id", "Expected one generated TARGET file")]
    [DataRow("wrong-job", "Expected one generated XLIFF file")]
    [DataRow("empty-target-zip", "Expected one TARGET file")]
    [DataRow("multiple-xliff-zip", "Expected one XLIFF file")]
    [DataRow("empty-xliff-download", "XTM returned an empty XLIFF file")]
    [DataRow("invalid-target-zip", "XTM returned an invalid TARGET archive")]
    [DataRow("error-target", "XTM could not generate the TARGET file: ERROR. Fixture generation failed.")]
    [DataRow("warning-xliff", "XTM could not generate the XLIFF file: WARNING. Fixture generation failed.")]
    [DataRow("source-mismatch", "Segment 1 differs between the translated and offline XLIFF.")]
    [DataRow("target-mismatch", "Segment 1 differs between the translated and offline XLIFF.")]
    [DataRow("language-mismatch", null)]
    [DataRow("count-mismatch", "The translated XLIFF contains 1 translatable segments, but XTM's offline XLIFF contains 0.")]
    [Timeout(30000)]
    public async Task Download_GeneratesBothFilesBeforePolling_AndValidatesResults(
        string scenario, string? expectedError)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de">
              <file id="f1"><unit id="u1"><segment id="s1">
                <source>Hello</source><target>Hallo</target>
              </segment></unit></file>
            </xliff>
            """;
        var offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2">
              <file source-language="en" target-language="de"><body><trans-unit id="t1">
                <source>Hello</source><target state="signed-off">Hallo</target>
              </trans-unit></body></file>
            </xliff>
            """;
        offline = scenario switch
        {
            "source-mismatch" => offline.Replace("<source>Hello</source>", "<source>Goodbye</source>"),
            "target-mismatch" => offline.Replace(">Hallo</target>", ">Guten Tag</target>"),
            "language-mismatch" => offline.Replace("target-language=\"de\"", "target-language=\"fr\""),
            "count-mismatch" => offline[..offline.IndexOf("<trans-unit", StringComparison.Ordinal)]
                + offline[(offline.IndexOf("</trans-unit>", StringComparison.Ordinal) + "</trans-unit>".Length)..],
            _ => offline,
        };
        var archives = new Dictionary<string, byte[]>();
        foreach (var fileType in new[] { "TARGET", "XLIFF" })
        {
            using var buffer = new MemoryStream();
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                var count = scenario == "empty-target-zip" && fileType == "TARGET" ? 0
                    : scenario == "multiple-xliff-zip" && fileType == "XLIFF" ? 2 : 1;
                for (var index = 0; index < count; index++)
                {
                    var entry = archive.CreateEntry(fileType == "TARGET" ? "translated.xlf" : $"offline-{index}.xlf");
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(Encoding.UTF8.GetBytes(fileType == "TARGET" ? target : offline));
                }
            }
            archives[fileType] = scenario == "empty-xliff-download" && fileType == "XLIFF" ? []
                : scenario == "invalid-target-zip" && fileType == "TARGET" ? Encoding.UTF8.GetBytes("invalid zip")
                : buffer.ToArray();
        }

        using var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        using var listener = new HttpListener();
        var baseUrl = $"http://127.0.0.1:{port}";
        listener.Prefixes.Add(baseUrl + "/");
        listener.Start();
        var requests = new List<string>();
        var generationRequests = new Dictionary<string, HttpListenerContext>();
        var statusCounts = new Dictionary<string, int> { ["TARGET"] = 0, ["XLIFF"] = 0 };
        var downloads = new List<string>();
        var server = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    var path = context.Request.Url!.AbsolutePath;
                    requests.Add(path);
                    byte[] response;
                    if (path == "/projects/123/files/generate")
                    {
                        Assert.AreEqual("POST", context.Request.HttpMethod);
                        Assert.AreEqual("42", context.Request.QueryString["jobIds"]);
                        var fileType = context.Request.QueryString["fileType"]!;
                        Assert.IsTrue(fileType is "TARGET" or "XLIFF");
                        Assert.IsTrue(generationRequests.TryAdd(fileType, context), "Each file type must be generated once.");
                        if (generationRequests.Count != 2)
                            continue;

                        // Neither request completes until both arrive: sequential generation would time out.
                        foreach (var (generatedType, generatedContext) in generationRequests)
                        {
                            var count = scenario == "missing-target" && generatedType == "TARGET" ? 0
                                : scenario == "duplicate-xliff" && generatedType == "XLIFF" ? 2 : 1;
                            var generated = Enumerable.Range(0, count).Select(_ => new
                            {
                                fileId = scenario == "empty-id" && generatedType == "TARGET" ? ""
                                    : generatedType == "TARGET" ? "271" : "982",
                                jobId = scenario == "wrong-job" && generatedType == "XLIFF" ? "99" : "42",
                                fileType = generatedType,
                            }).ToArray();
                            var bytes = JsonSerializer.SerializeToUtf8Bytes(generated);
                            generatedContext.Response.ContentType = "application/json";
                            generatedContext.Response.ContentLength64 = bytes.Length;
                            await generatedContext.Response.OutputStream.WriteAsync(bytes);
                            generatedContext.Response.Close();
                        }
                        continue;
                    }

                    Assert.AreEqual(2, generationRequests.Count, "Both generations must precede the first status request.");
                    Assert.AreEqual("GET", context.Request.HttpMethod);
                    Assert.AreEqual("JOB", context.Request.QueryString["fileScope"]);
                    var requestedType = path.Contains("/271/", StringComparison.Ordinal) ? "TARGET"
                        : path.Contains("/982/", StringComparison.Ordinal) ? "XLIFF" : null;
                    Assert.IsNotNull(requestedType, $"Unexpected endpoint: {path}. Download the generated file IDs.");
                    if (path == $"/projects/123/files/{(requestedType == "TARGET" ? "271" : "982")}/status")
                    {
                        statusCounts[requestedType]++;
                        var status = scenario == "error-target" && requestedType == "TARGET" ? "ERROR"
                            : scenario == "warning-xliff" && requestedType == "XLIFF" ? "WARNING"
                            : scenario == requestedType.ToLowerInvariant() + "-pending" && statusCounts[requestedType] == 1
                                ? "IN_PROGRESS" : "FINISHED";
                        response = JsonSerializer.SerializeToUtf8Bytes(new { status, message = "Fixture generation failed." });
                        context.Response.ContentType = "application/json";
                    }
                    else
                    {
                        Assert.AreEqual($"/projects/123/files/{(requestedType == "TARGET" ? "271" : "982")}/download", path);
                        Assert.IsTrue(statusCounts.Values.All(x => x > 0));
                        if (scenario.EndsWith("-pending", StringComparison.Ordinal))
                            Assert.AreEqual(2, statusCounts[scenario.StartsWith("target", StringComparison.Ordinal) ? "TARGET" : "XLIFF"],
                                "Downloads must wait for both generations to finish.");
                        downloads.Add(requestedType);
                        response = archives[requestedType];
                        context.Response.ContentType = "application/zip";
                    }
                    context.Response.ContentLength64 = response.Length;
                    await context.Response.OutputStream.WriteAsync(response);
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (!listener.IsListening)
            {
            }
            catch (ObjectDisposedException) when (!listener.IsListening)
            {
            }
        });
        byte[]? uploadedBytes = null;
        var fileManager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        fileManager.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async (Stream stream, string contentType, string fileName) =>
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                uploadedBytes = copy.ToArray();
                return new FileReference { Name = fileName, ContentType = contentType };
            });
        var warnings = new List<string?>();
        var invocation = new InvocationContext
        {
            Logger = new("Warning")
            {
                LogWarning = (message, _) => warnings.Add(message),
            },
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredsNames.ConnectionType, ConnectionTypes.GeneratedToken),
                new AuthenticationCredentialsProvider(CredsNames.Url, baseUrl),
                new AuthenticationCredentialsProvider(CredsNames.Token, "local-test-token"),
            ],
        };
        var actions = new InteroperableActions(invocation, fileManager.Object);
        try
        {
            var action = actions.DownloadTranslatedInteroperableFile(new ProjectRequest { ProjectId = "123" },
                new DownloadTranslatedInteroperableFileRequest
                {
                    JobId = "42",
                    AttributeSegmentsToUser = "none",
                    ProvenanceType = "translation",
                });
            if (expectedError is not null)
            {
                var exception = await Assert.ThrowsAsync<PluginApplicationException>(async () =>
                    await action.WaitAsync(TimeSpan.FromSeconds(20)));
                StringAssert.Contains(exception.Message, expectedError);
                fileManager.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                if (scenario.EndsWith("-mismatch", StringComparison.Ordinal))
                {
                    Assert.HasCount(1, warnings);
                    Assert.AreEqual(
                        $"[XTM_DownloadTranslatedInteroperableFile] Provenance mapping failed for project 123, job 42: {exception.Message}",
                        warnings.Single(), "The warning must preserve the mapping reason and identify the affected job.");
                }
            }
            else
            {
                var result = await action.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.AreEqual("translated.xlf", result.File.Name);
                Assert.AreEqual("application/xliff+xml", result.File.ContentType);
                Assert.IsNotNull(uploadedBytes);
                using var output = new MemoryStream(uploadedBytes);
                var loaded = Transformation.Load(output, result.File.Name, result.File.ContentType);
                Assert.IsTrue(loaded.Success, loaded.Error);
                var unit = loaded.Value!.GetUnits().Single();
                Assert.AreEqual("XTM", unit.Provenance.Translation.Tool);
                Assert.IsNull(unit.Provenance.Translation.Person, "The no-attribution mode must not include an assigned person.");
                var xml = XDocument.Parse(Encoding.UTF8.GetString(uploadedBytes));
                Assert.AreEqual("en", (string?)xml.Root!.Attribute("srcLang"));
                Assert.AreEqual("de", (string?)xml.Root.Attribute("trgLang"));
                Assert.AreEqual("XTM", xml.Descendants().Single(x => x.Name.LocalName == "unit")
                    .Attribute(XName.Get("tool", "http://www.w3.org/2005/11/its"))?.Value,
                    "Provenance must be serialized on the unit.");
                Assert.AreEqual("Hallo", xml.Descendants().Single(x => x.Name.LocalName == "target").Value);
                CollectionAssert.AreEquivalent(new[] { "TARGET", "XLIFF" }, downloads);
                Assert.AreEqual(scenario == "target-pending" ? 2 : 1, statusCounts["TARGET"]);
                Assert.AreEqual(scenario == "xliff-pending" ? 2 : 1, statusCounts["XLIFF"]);
                fileManager.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "application/xliff+xml", "translated.xlf"), Times.Once);
            }
            if (expectedError is null || !scenario.EndsWith("-mismatch", StringComparison.Ordinal))
                Assert.IsEmpty(warnings, "Successful mapping and unrelated download errors must not emit mapping warnings.");
            Assert.AreEqual(2, generationRequests.Count);
            Assert.IsTrue(requests.Take(2).All(x => x == "/projects/123/files/generate"));
            Assert.IsFalse(requests.Any(x => x.Contains("workflow", StringComparison.Ordinal)),
                "Explicit provenance type and no-attribution mode should skip workflow lookup.");
        }
        finally
        {
            listener.Stop();
            await server;
        }
    }
}
