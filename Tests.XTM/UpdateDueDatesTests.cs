using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Tests.XTM;

[TestClass]
public class UpdateDueDatesTests
{
    [TestMethod]
    [DataRow("projectDueDate", null, 200, DateTimeKind.Utc)]
    [DataRow("newSourceDueDate", null, 200, DateTimeKind.Unspecified)]
    [DataRow("workflowStartDate", null, 200, DateTimeKind.Local)]
    [DataRow("workflowDueDate", "  ", 200, DateTimeKind.Utc)]
    [DataRow("steps", " correct1 ", 200, DateTimeKind.Utc)]
    [DataRow("jobs", " 3000000000 ", 200, DateTimeKind.Utc)]
    [DataRow("targetLanguages", " fr_FR ", 200, DateTimeKind.Utc)]
    [DataRow("projectDueDate", null, 400, DateTimeKind.Utc)]
    [DataRow("projectDueDate", null, 403, DateTimeKind.Utc)]
    [DataRow("projectDueDate", null, 404, DateTimeKind.Utc)]
    public async Task UpdateDueDates_UsesExpectedContract(string type, string? target, int status, DateTimeKind kind)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var listener = new HttpListener();
        var url = $"http://127.0.0.1:{port}";
        listener.Prefixes.Add(url + "/");
        listener.Start();
        var context = new InvocationContext
        {
            AuthenticationCredentialsProviders = new[]
            {
                new AuthenticationCredentialsProvider(CredsNames.ConnectionType, ConnectionTypes.GeneratedToken),
                new AuthenticationCredentialsProvider(CredsNames.Url, url),
                new AuthenticationCredentialsProvider(CredsNames.Token, "test-token")
            }
        };
        var date = new DateTime(2030, 6, 15, 17, 23, 45, kind);
        var call = new WorkflowActions(context).UpdateDueDates(new() { ProjectId = "123" },
            new() { Type = type, Date = date, TargetIdentifier = target });
        var request = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var rawBody = await new StreamReader(request.Request.InputStream).ReadToEndAsync();
        var body = JObject.Parse(rawBody);
        request.Response.StatusCode = status;
        request.Response.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(status == 200 ? "{\"success\":true}" : "{\"reason\":\"Test failure\"}");
        await request.Response.OutputStream.WriteAsync(bytes);
        request.Response.Close();

        Assert.AreEqual("PUT", request.Request.HttpMethod);
        Assert.AreEqual("/projects/123/due-dates", request.Request.Url!.PathAndQuery);
        Assert.AreEqual("XTM-Basic test-token", request.Request.Headers["Authorization"]);
        Assert.AreEqual(1, body.Count);
        var utc = kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();
        var expected = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        StringAssert.Contains(rawBody, expected);
        if (type is "steps" or "jobs" or "targetLanguages")
        {
            var entries = (JArray)body[type]!;
            Assert.AreEqual(1, entries.Count);
            var entry = (JObject)entries[0];
            Assert.AreEqual(2, entry.Count);
            Assert.AreEqual(utc, entry["dueDate"]!.Value<DateTime>());
            var key = type == "steps" ? "referenceStepName" : type == "jobs" ? "id" : "languageCode";
            Assert.AreEqual(target!.Trim(), entry[key]!.ToString());
            if (type == "jobs") Assert.AreEqual(JTokenType.Integer, entry[key]!.Type);
        }
        else
            Assert.AreEqual(utc, body[type]!.Value<DateTime>());

        if (status == 200)
            Assert.IsTrue((await call).Success);
        else
        {
            var error = await Assert.ThrowsAsync<PluginApplicationException>(async () => await call);
            StringAssert.Contains(error.Message, "Test failure");
        }
    }

    [TestMethod]
    [DataRow("steps", null, false, "123")]
    [DataRow("jobs", " ", false, "123")]
    [DataRow("targetLanguages", null, false, "123")]
    [DataRow("jobs", "0", false, "123")]
    [DataRow("jobs", "-1", false, "123")]
    [DataRow("jobs", "abc", false, "123")]
    [DataRow("jobs", "9223372036854775808", false, "123")]
    [DataRow("projectDueDate", "123", false, "123")]
    [DataRow("newSourceDueDate", "123", false, "123")]
    [DataRow("workflowStartDate", "123", false, "123")]
    [DataRow("workflowDueDate", "123", false, "123")]
    [DataRow("unknown", null, false, "123")]
    [DataRow("projectDueDate", null, true, "123")]
    [DataRow("projectDueDate", null, false, "invalid")]
    public async Task UpdateDueDates_RejectsInvalidInputBeforeHttp(string type, string? target, bool missingDate, string projectId)
    {
        var action = new WorkflowActions(new InvocationContext
        {
            AuthenticationCredentialsProviders = Array.Empty<AuthenticationCredentialsProvider>()
        });
        await Assert.ThrowsAsync<PluginMisconfigurationException>(() => action.UpdateDueDates(
            new ProjectRequest { ProjectId = projectId }, new UpdateDueDatesRequest
            {
                Type = type, TargetIdentifier = target,
                Date = missingDate ? default : new DateTime(2030, 6, 15)
            }));
    }
}
