using System.Security.Cryptography;
using System.Text;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Polling.Models.Memory;
using Apps.XTM.Polling.Models.Response;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.XTM.Polling;

[PollingEventList]
public class WorkflowStepCompletedPollingList(InvocationContext invocationContext) : XtmInvocable(invocationContext)
{
    private const int PageSize = 100;
    private const int MaxEventsPerPoll = 100;

    [PollingEvent("On workflow step completed (polling)",
        "Triggers when all jobs in an active project, or all jobs for a target language, have finished the selected step"),
     MultipleEvents]
    public async Task<PollingEventResponse<WorkflowStepCompletedMemory, List<WorkflowStepCompletedResponse>>> OnWorkflowStepCompleted(
        PollingEventRequest<WorkflowStepCompletedMemory> request,
        [PollingEventParameter] WorkflowStepCompletedPollingRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.WorkflowStep))
            throw new PluginMisconfigurationException("Workflow step is required.");

        var scope = string.IsNullOrWhiteSpace(input.CompletionScope)
            ? CompletionScopeDataHandler.Project
            : input.CompletionScope;
        if (scope != CompletionScopeDataHandler.Project && scope != CompletionScopeDataHandler.TargetLanguage)
            throw new PluginMisconfigurationException("Select Entire project or Each target language as the completion scope.");

        var projectIds = input.ProjectIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray() ?? [];
        var step = NormalizeStepName(input.WorkflowStep);
        var configuration = JsonConvert.SerializeObject(new
        {
            Instance = Url.TrimEnd('/'),
            Client = Creds.FirstOrDefault(x => x.KeyName == CredsNames.Client)?.Value,
            User = Creds.FirstOrDefault(x => x.KeyName == CredsNames.UserId)?.Value,
            Step = step,
            Scope = scope,
            ProjectIds = projectIds,
            Name = input.ProjectNameContains ?? string.Empty
        });
        var memory = new WorkflowStepCompletedMemory
        {
            Configuration = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configuration)))
        };
        var isBaseline = request.Memory?.Configuration != memory.Configuration;
        var results = new List<WorkflowStepCompletedResponse>();
        var token = await Client.GetToken(Creds);
        var query = new Dictionary<string, string?>
        {
            ["activity"] = "ACTIVE",
            ["pageSize"] = PageSize.ToString(),
            ["sort"] = "id,asc"
        };
        if (projectIds.Length > 0)
            query["ids"] = string.Join(",", projectIds);

        for (var page = 1; ; page++)
        {
            query["page"] = page.ToString();
            var endpoint = QueryHelpers.AddQueryString(ApiEndpoints.Projects, query);
            var projects = await GetResponse<List<SimpleProject>>(endpoint, token);
            if (projects.Count == 0)
                break;

            // Filter and process this page once; do not build a second project collection.
            foreach (var project in projects)
            {
                if (!string.Equals(project.Activity, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(input.ProjectNameContains)
                    && project.Name?.Contains(input.ProjectNameContains, StringComparison.Ordinal) != true)
                    continue;
                if (memory.CompletedGroups.ContainsKey(project.Id))
                    continue;

                var statusEndpoint = $"{ApiEndpoints.Projects}/{Uri.EscapeDataString(project.Id)}/status?fetchLevel=STEPS";
                var status = await GetResponse<ProjectDetailedStatusResponse>(statusEndpoint, token);
                if (status.Jobs is null)
                    throw new PluginApplicationException("XTM returned no job collection for a project status.");

                var handledGroups = new HashSet<string>(StringComparer.Ordinal);
                memory.CompletedGroups.Add(project.Id, handledGroups);
                HashSet<string>? previousGroups = null;
                request.Memory?.CompletedGroups.TryGetValue(project.Id, out previousGroups);

                foreach (var (groupKey, group) in GetGroups(status.Jobs, scope, step))
                {
                    if (!group.IsComplete)
                        continue;

                    if (isBaseline || previousGroups?.Contains(groupKey) == true)
                    {
                        handledGroups.Add(groupKey);
                        continue;
                    }

                    if (results.Count >= MaxEventsPerPoll)
                        continue; // Not handled yet: retry this completion on the next poll.

                    results.Add(new WorkflowStepCompletedResponse
                    {
                        ProjectId = project.Id,
                        WorkflowStep = input.WorkflowStep.Trim(),
                        TargetLanguage = scope == CompletionScopeDataHandler.TargetLanguage ? groupKey : null,
                        JobIds = group.JobIds.ToArray()
                    });

                    handledGroups.Add(groupKey);
                }
            }
        }

        return new()
        {
            Memory = memory,
            FlyBird = results.Count > 0,
            Result = results.Count > 0 ? results : null
        };
    }

    private async Task<T> GetResponse<T>(string endpoint, string token) where T : class
    {
        var response = await Client.ExecuteXtm<T>(new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + endpoint,
            Method = Method.Get
        }, token));
        return response ?? throw new PluginApplicationException("XTM returned an empty response.");
    }

    private static Dictionary<string, CompletionGroup> GetGroups(
        List<ProjectDetailedStatusWorkflowJob> jobs, string scope, string selectedStep)
    {
        var groups = new Dictionary<string, CompletionGroup>(StringComparer.Ordinal);

        foreach (var job in jobs)
        {
            if (string.Equals(job.CompletionStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(job.CompletionStatus, "DELETED", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(job.JobId))
                throw new PluginApplicationException("XTM returned a job without an ID.");

            if (scope == CompletionScopeDataHandler.TargetLanguage && string.IsNullOrWhiteSpace(job.TargetLanguage))
                throw new PluginApplicationException("XTM returned a job without a target language.");

            var key = scope == CompletionScopeDataHandler.TargetLanguage ? job.TargetLanguage : "all";
            if (!groups.TryGetValue(key, out var group))
            {
                group = new CompletionGroup();
                groups.Add(key, group);
            }

            group.JobIds.Add(job.JobId);

            var validJobStatus = string.Equals(job.CompletionStatus, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(job.CompletionStatus, "FINISHED", StringComparison.OrdinalIgnoreCase);

            var finishedStep = job.Steps?.Any(s =>
                string.Equals(s.Status, "FINISHED", StringComparison.OrdinalIgnoreCase)
                && (NormalizeStepName(s.StepReferenceName) == selectedStep
                    || NormalizeStepName(s.WorkflowStepName) == selectedStep
                    || NormalizeStepName(s.DisplayStepName) == selectedStep)) == true;

            if (!validJobStatus || !finishedStep)
                group.IsComplete = false;
        }

        return groups;
    }

    private static string NormalizeStepName(string? name) =>
        string.Concat((name ?? string.Empty).Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    private sealed class CompletionGroup
    {
        public bool IsComplete { get; set; } = true;
        public HashSet<string> JobIds { get; } = new(StringComparer.Ordinal);
    }
}
