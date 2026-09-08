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
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Moq;

namespace Tests.XTM;

[TestClass]
public class InteroperableWorkflowTests
{
    [TestMethod]
    [DataRow("AUTOMATIC_ACTIONS", "IN_PROGRESS", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("AUTOMATIC_ACTIONS", "FINISHED", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("EXTERNAL_AUTOMATIC", "IN_PROGRESS", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("EXTERNAL_AUTOMATIC", "FINISHED", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("OUTER_AUTOMATIC", "IN_PROGRESS", null, false, "stepReferenceName", "FINISHED")]
    [DataRow(null, "IN_PROGRESS", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("missing-definition", "FINISHED", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("UNKNOWN_TYPE", "IN_PROGRESS", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("AUTOMATIC_ACTIONS", "IN_PROGRESS", "tm1", true, "stepReferenceName", "FINISHED")]
    [DataRow(null, "IN_PROGRESS", "tm1", true, "stepReferenceName", "FINISHED")]
    [DataRow("missing-definition", "FINISHED", "tm1", true, "stepReferenceName", "FINISHED")]
    [DataRow("UNKNOWN_TYPE", "IN_PROGRESS", "tm1", true, "stepReferenceName", "FINISHED")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, "referenceStepName", "FINISHED")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, "referenceStepName", "IN_PROGRESS")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, "stepReferenceName", "FINISHED")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, "stepReferenceName", "IN_PROGRESS")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, null, "FINISHED")]
    [DataRow("ONLINE_TRANSLATION", "NOT_STARTED", null, false, null, "IN_PROGRESS")]
    [Timeout(30000)]
    public async Task Download_WorkflowAttribution_SelectsOnlyEligibleManualStep(
        string? trailingType, string trailingStatus, string? workflowOverride, bool expectedError,
        string? statusReferenceField, string reviewStatus)
    {
        var repeatedReview = trailingType == "ONLINE_TRANSLATION";
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de">
              <file id="f"><unit id="u"><segment id="s"><source>Hello</source><target>Hallo</target></segment></unit></file>
            </xliff>
            """;
        const string offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body>
              <trans-unit id="t1"><source>Hello</source><target state="signed-off" state-qualifier="exact-match">Hallo</target></trans-unit>
            </body></file></xliff>
            """;
        var archives = new Dictionary<string, byte[]>();
        foreach (var fileType in new[] { "TARGET", "XLIFF" })
        {
            using var buffer = new MemoryStream();
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(fileType == "TARGET" ? "translated.xlf" : "offline.xlf");
                await using var stream = entry.Open();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(fileType == "TARGET" ? target : offline));
            }
            archives[fileType] = buffer.ToArray();
        }

        using var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        using var listener = new HttpListener();
        var baseUrl = $"http://127.0.0.1:{port}";
        listener.Prefixes.Add(baseUrl + "/");
        listener.Start();
        var generations = new Dictionary<string, HttpListenerContext>();
        var requests = new List<string>();
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
                    context.Response.ContentType = "application/json";
                    if (path == "/projects/123/files/generate")
                    {
                        var fileType = context.Request.QueryString["fileType"]!;
                        Assert.IsTrue(generations.TryAdd(fileType, context));
                        if (generations.Count != 2)
                            continue;
                        foreach (var (generatedType, generation) in generations)
                        {
                            var bytes = JsonSerializer.SerializeToUtf8Bytes(new[]
                            {
                                new { fileId = generatedType == "TARGET" ? "271" : "982", jobId = "42", fileType = generatedType },
                            });
                            generation.Response.ContentLength64 = bytes.Length;
                            await generation.Response.OutputStream.WriteAsync(bytes);
                            generation.Response.Close();
                        }
                        continue;
                    }

                    Assert.AreEqual(2, generations.Count, "Both generations must start before workflow lookup.");
                    switch (path)
                    {
                        case "/projects/123/workflow":
                            Assert.AreEqual("42", context.Request.QueryString["jobIds"]);
                            response = JsonSerializer.SerializeToUtf8Bytes(new[]
                            {
                                new { steps = new[]
                                {
                                    new { id = "1", name = "translate", displayStepName = "translate1", referenceStepName = "translate1" },
                                    new { id = "2", name = "review", displayStepName = "review1", referenceStepName = "review1" },
                                    new { id = "3", name = repeatedReview ? "review" : "TM",
                                        displayStepName = repeatedReview ? "review2" : "TM1",
                                        referenceStepName = repeatedReview ? "review2" : "tm1" },
                                } },
                            });
                            break;
                        case "/workflows/steps":
                            response = JsonSerializer.SerializeToUtf8Bytes(new[]
                            {
                                new { id = "1", name = "translate", type = "ONLINE_TRANSLATION", role = "TRANSLATE" },
                                new { id = "2", name = "review", type = "ONLINE_TRANSLATION", role = "REVIEW" },
                                new { id = "3", name = repeatedReview ? "review" : "TM", type = trailingType,
                                    role = repeatedReview ? "REVIEW" : "TRANSLATE" },
                            }.Where(x => trailingType != "missing-definition" || x.id != "3"));
                            break;
                        case "/projects/123/status":
                            Assert.AreEqual("STEPS", context.Request.QueryString["fetchLevel"]);
                            var statusSteps = new[]
                            {
                                new Dictionary<string, string>
                                {
                                    ["workflowStepName"] = "translate", ["displayStepName"] = "translate1",
                                    ["status"] = "FINISHED",
                                },
                                new Dictionary<string, string>
                                {
                                    ["workflowStepName"] = "review", ["displayStepName"] = "review1",
                                    ["status"] = reviewStatus,
                                },
                                new Dictionary<string, string>
                                {
                                    ["workflowStepName"] = repeatedReview ? "review" : "TM",
                                    ["displayStepName"] = repeatedReview ? "review2" : "TM1",
                                    ["status"] = trailingStatus,
                                },
                            };
                            if (statusReferenceField is not null)
                            {
                                foreach (var step in statusSteps)
                                    step[statusReferenceField] = step["displayStepName"].ToLowerInvariant();
                            }
                            response = JsonSerializer.SerializeToUtf8Bytes(new { jobs = new[]
                            {
                                new { jobId = "42", steps = statusSteps },
                            } });
                            break;
                        case "/projects/123/workflow/assignment":
                            Assert.AreEqual("42", context.Request.QueryString["jobIds"]);
                            response = JsonSerializer.SerializeToUtf8Bytes(new { jobs = new[]
                            {
                                new { jobId = "99", steps = new[]
                                {
                                    new { name = "review", displayStepName = "review1", referenceStepName = "review1",
                                        bundles = new[] { new { from = 1, to = 1, userId = "999", userName = "Other job" } } },
                                } },
                                new { jobId = "42", steps = new[]
                                {
                                    new { name = "review", displayStepName = "review2", referenceStepName = statusReferenceField is null ? null : "review2",
                                        bundles = new[] { new { from = 1, to = 1, userId = "200", userName = "Other reviewer" } } },
                                    new { name = "review", displayStepName = "review1", referenceStepName = statusReferenceField is null ? null : "review1",
                                        bundles = new[] { new { from = 1, to = 1, userId = "100", userName = "Selected reviewer" } } },
                                    new { name = "TM", displayStepName = "TM1", referenceStepName = "tm1",
                                        bundles = new[] { new { from = 1, to = 1, userId = "300", userName = "Automatic assignee" } } },
                                } },
                            } });
                            break;
                        case "/projects/123/files/271/status":
                        case "/projects/123/files/982/status":
                            response = JsonSerializer.SerializeToUtf8Bytes(new { status = "FINISHED" });
                            break;
                        case "/projects/123/files/271/download":
                        case "/projects/123/files/982/download":
                            response = archives[path.Contains("/271/", StringComparison.Ordinal) ? "TARGET" : "XLIFF"];
                            context.Response.ContentType = "application/zip";
                            break;
                        default:
                            throw new AssertFailedException($"Unexpected endpoint: {path}");
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

        byte[]? uploaded = null;
        var fileManager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        fileManager.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async (Stream stream, string contentType, string name) =>
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                uploaded = copy.ToArray();
                return new FileReference { Name = name, ContentType = contentType };
            });
        var invocation = new InvocationContext
        {
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredsNames.ConnectionType, ConnectionTypes.GeneratedToken),
                new AuthenticationCredentialsProvider(CredsNames.Url, baseUrl),
                new AuthenticationCredentialsProvider(CredsNames.Token, "local-test-token"),
            ],
        };
        try
        {
            var action = new InteroperableActions(invocation, fileManager.Object).DownloadTranslatedInteroperableFile(
                new ProjectRequest { ProjectId = "123" },
                new DownloadTranslatedInteroperableFileRequest { JobId = "42", WorkflowStep = workflowOverride });
            if (expectedError)
            {
                await Assert.ThrowsAsync<PluginMisconfigurationException>(async () =>
                    await action.WaitAsync(TimeSpan.FromSeconds(20)));
                Assert.IsNull(uploaded);
                Assert.IsFalse(requests.Contains("/projects/123/workflow/assignment"));
            }
            else
            {
                await action.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.IsNotNull(uploaded);
                var output = XDocument.Parse(Encoding.UTF8.GetString(uploaded));
                var unit = output.Descendants().Single(x => x.Name.LocalName == "unit");
                XNamespace its = "http://www.w3.org/2005/11/its";
                Assert.AreEqual("Selected reviewer (ID 100)", unit.Attribute(its + "revPerson")?.Value,
                    "The latest active or finished manual reviewer must receive attribution, without selecting automatic or future steps.");
                Assert.AreEqual("XTM", unit.Attribute(its + "revTool")?.Value);
                Assert.IsNull(unit.Attribute(its + "person"), "The selected reviewer's REVIEW role must determine provenance type.");
                Assert.IsNull(unit.Attribute(its + "tool"));
            }
        }
        finally
        {
            listener.Stop();
            await server;
        }
    }

    [TestMethod]
    [DataRow("{\"referenceStepName\":\"review1\"}")]
    [DataRow("{\"stepReferenceName\":\"review1\"}")]
    [DataRow("{\"referenceStepName\":\"review1\",\"stepReferenceName\":\"review2\"}")]
    [DataRow("{\"stepReferenceName\":\"review2\",\"referenceStepName\":\"review1\"}")]
    public void WorkflowStatus_DeserializesReferenceNames_WithDocumentedFieldTakingPrecedence(string json)
    {
        var step = Newtonsoft.Json.JsonConvert.DeserializeObject<ProjectDetailedStatusWorkflowStep>(json);

        Assert.IsNotNull(step);
        Assert.AreEqual("review1", step.StepReferenceName);
    }
}
