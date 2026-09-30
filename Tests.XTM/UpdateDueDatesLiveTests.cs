using System.Text;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using RestSharp;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class UpdateDueDatesLiveTests : TestBaseMultipleConnections
{
    [TestMethod, TestCategory("Live"), Timeout(180000)]
    public async Task UpdateDueDates_VerifiesAllDateTypesAndStartDateBoundary()
    {
        if (Environment.GetEnvironmentVariable("XTM_VERIFY_DUE_DATES") != "1")
            Assert.Inconclusive("Set XTM_VERIFY_DUE_DATES=1 and XTM_DUE_DATES_CUSTOMER_ID to create and retain a live test project.");
        var customerId = Environment.GetEnvironmentVariable("XTM_DUE_DATES_CUSTOMER_ID");
        Assert.IsFalse(string.IsNullOrWhiteSpace(customerId), "XTM_DUE_DATES_CUSTOMER_ID is required.");
        var context = GetInvocationContext(ConnectionTypes.Credentials);
        var creds = context.AuthenticationCredentialsProviders.ToArray();
        using var client = new XTMClient();
        var projectId = Environment.GetEnvironmentVariable("XTM_DUE_DATES_PROJECT_ID");
        if (string.IsNullOrWhiteSpace(projectId))
        {
            var workflows = await client.ExecuteXtmWithJson<JArray>("/workflows", Method.Get, null, creds);
            var workflow = workflows.First(x => x["name"]!.Value<string>() == "translate -> correct");
            var baseUrl = creds.First(x => x.KeyName == CredsNames.Url).Value;
            var request = new XTMRequest(new()
            {
                Url = baseUrl + "/projects", Method = Method.Post
            }, await client.GetToken(creds)) { AlwaysMultipartFormData = true };
            request.AddParameter("name", "Blackbird due dates verification " + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            request.AddParameter("customerId", customerId);
            request.AddParameter("workflowId", workflow["id"]!.ToString());
            request.AddParameter("sourceLanguage", "en_US");
            request.AddParameter("targetLanguages", "fr_FR");
            request.AddParameter("sendEmailOption", "DISABLED_FULLY");
            request.AddFile("translationFiles[0].file", Encoding.UTF8.GetBytes("This file verifies due date updates."), "due-dates.txt", "text/plain");
            var created = await client.ExecuteXtm<JObject>(request);
            projectId = created["projectId"]!.ToString();
        }
        var fixture = await client.ExecuteXtmWithJson<JObject>($"/projects/{projectId}", Method.Get, null, creds);
        StringAssert.StartsWith(fixture["name"]!.ToString(), "Blackbird due dates verification ");
        TestContext.WriteLine("Retained test project ID: " + projectId);
        JObject status = new();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            status = await client.ExecuteXtmWithJson<JObject>($"/projects/{projectId}/status?fetchLevel=STEPS", Method.Get, null, creds);
            if (status["jobs"] is JArray jobs && jobs.Count > 0 && jobs[0]["steps"] is JArray steps && steps.Count > 0)
                break;
            await Task.Delay(2000);
        }
        var job = status["jobs"]!.First!;
        var stepName = job["steps"]!.First!["referenceStepName"]!.ToString();
        var action = new WorkflowActions(context);
        var baseline = DateTime.UtcNow.Date.AddHours(17);
        foreach (var (type, target) in new (string, string?)[]
        {
            ("projectDueDate", null), ("workflowStartDate", null), ("newSourceDueDate", null),
            ("workflowDueDate", null), ("steps", stepName),
            ("jobs", job["jobId"]!.ToString()), ("targetLanguages", "fr_FR")
        })
        {
            var date = baseline.AddDays(type switch
            {
                "projectDueDate" => 40, "newSourceDueDate" => 2,
                "workflowStartDate" => 1, "workflowDueDate" => 30,
                "steps" => 10, "jobs" => 11, _ => 12
            });
            Apps.XTM.Models.Response.Workflows.UpdateDueDatesResponse? response = null;
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    response = await action.UpdateDueDates(new ProjectRequest { ProjectId = projectId },
                        new UpdateDueDatesRequest { Type = type, Date = date, TargetIdentifier = target });
                    break;
                }
                catch (PluginApplicationException ex) when (attempt < 29 && ex.Message.Contains("Project is under analysis"))
                {
                    await Task.Delay(2000);
                }
            }
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Success, type);
            var details = await client.ExecuteXtmWithJson<JObject>($"/projects/{projectId}", Method.Get, null, creds);
            var readback = await client.ExecuteXtmWithJson<JObject>($"/projects/{projectId}/status?fetchLevel=STEPS", Method.Get, null, creds);
            TestContext.WriteLine($"{type}: success; project dates: " + string.Join(", ", details.Properties()
                .Where(x => x.Name.Contains("due", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("workflow", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Name + "=" + x.Value)));
            TestContext.WriteLine("Step date readback: " + string.Join(", ", readback.SelectTokens("$.jobs[*].steps[*].dueDate")));
            var expected = new DateTimeOffset(date).ToUnixTimeMilliseconds();
            if (type is "projectDueDate" or "newSourceDueDate")
                Assert.AreEqual(expected, details[type == "projectDueDate" ? "dueDate" : type]!.Value<long>());
            else if (type is "steps" or "jobs" or "targetLanguages")
            {
                var updatedJob = readback["jobs"]!.First!;
                var updatedStep = type == "steps"
                    ? updatedJob["steps"]!.First(x => x["referenceStepName"]!.ToString() == target)
                    : updatedJob["steps"]!.Last!;
                Assert.AreEqual(expected, updatedStep["dueDate"]!.Value<long>(), type);
            }
            else
            {
                Assert.IsNotNull(details[type], $"XTM must expose the persisted {type}.");
                Assert.AreEqual(expected, details[type]!.Value<long>(), type);
            }
        }

        // XTM requires workflow start to be strictly earlier than preprocessing due date.
        // Verify the boundary and that rejected updates preserve the previous value.
        var project = new ProjectRequest { ProjectId = projectId };
        var preprocessingDueDate = baseline.AddDays(2);
        try
        {
            foreach (var seconds in new[] { -1, 0, 1 })
            {
                var input = new UpdateDueDatesRequest
                {
                    Type = "workflowStartDate", Date = preprocessingDueDate.AddSeconds(seconds)
                };
                if (seconds < 0)
                    Assert.IsTrue((await action.UpdateDueDates(project, input)).Success);
                else
                {
                    var error = await Assert.ThrowsAsync<PluginApplicationException>(() => action.UpdateDueDates(project, input));
                    Assert.AreEqual("Error: Incorrect parameters were passed: date.", error.Message);
                }

                var details = await client.ExecuteXtmWithJson<JObject>($"/projects/{projectId}", Method.Get, null, creds);
                Assert.AreEqual(new DateTimeOffset(preprocessingDueDate.AddSeconds(-1)).ToUnixTimeMilliseconds(),
                    details["workflowStartDate"]!.Value<long>());
                TestContext.WriteLine($"Workflow start {seconds:+0;-0;0}s relative to preprocessing due date: boundary verified.");
            }
        }
        finally
        {
            Assert.IsTrue((await action.UpdateDueDates(project,
                new UpdateDueDatesRequest { Type = "workflowStartDate", Date = baseline.AddDays(1) })).Success);
        }
    }
}
