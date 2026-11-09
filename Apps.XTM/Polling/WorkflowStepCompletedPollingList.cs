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

    [PollingEvent("On workflow steps completed (polling)",
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
        var previousMemory = isBaseline ? null : request.Memory;
        var projectsToRetry = new HashSet<string>(StringComparer.Ordinal);
        var projectsToBaseline = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<WorkflowStepCompletedResponse>();
        if (previousMemory is not null)
        {
            foreach (var pending in previousMemory.PendingCompletions)
            {
                if (results.Count < MaxEventsPerPoll)
                    results.Add(pending);
                else
                    memory.PendingCompletions.Add(pending);
            }
        }

        var resumeAfter = previousMemory?.LastScannedProjectId;
        var lastScannedProjectId = resumeAfter;
        var scanDeferred = false;
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

                List<string>? previousCompletedGroups = null;
                previousMemory?.CompletedGroups.TryGetValue(project.Id, out previousCompletedGroups);
                var previousGroups = previousCompletedGroups?.ToHashSet(StringComparer.Ordinal);
                memory.CompletedGroups.Add(project.Id, previousGroups?.ToList() ?? []);
                if (previousMemory?.ProjectsToRetry.Contains(project.Id) == true)
                    projectsToRetry.Add(project.Id);
                if (isBaseline || previousMemory?.ProjectsToBaseline.Contains(project.Id) == true)
                    projectsToBaseline.Add(project.Id);

                var projectId = ParseId(project.Id);
                if (projectId <= resumeAfter && !projectsToRetry.Contains(project.Id))
                    continue;
                if (results.Count >= MaxEventsPerPoll)
                {
                    scanDeferred = true;
                    continue;
                }

                try
                {
                    var statusEndpoint = $"{ApiEndpoints.Projects}/{Uri.EscapeDataString(project.Id)}/status?fetchLevel=STEPS";
                    var status = await GetResponse<ProjectDetailedStatusResponse>(statusEndpoint, token);
                    if (status.Jobs is null)
                        throw new PluginApplicationException("XTM returned no job collection for a project status.");

                    var groups = GetGroups(status.Jobs, scope, step);
                    var handledGroups = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var (groupKey, group) in groups)
                    {
                        if (!group.IsComplete)
                            continue;

                        if (!projectsToBaseline.Contains(project.Id) && previousGroups?.Contains(groupKey) != true)
                        {
                            var completion = new WorkflowStepCompletedResponse
                            {
                                ProjectId = project.Id,
                                WorkflowStep = input.WorkflowStep.Trim(),
                                TargetLanguage = scope == CompletionScopeDataHandler.TargetLanguage ? groupKey : null,
                                JobIds = group.JobIds.ToArray()
                            };
                            if (results.Count < MaxEventsPerPoll)
                                results.Add(completion);
                            else
                                memory.PendingCompletions.Add(completion);
                        }
                        handledGroups.Add(groupKey);
                    }

                    memory.CompletedGroups[project.Id] = handledGroups.ToList();
                    projectsToRetry.Remove(project.Id);
                    projectsToBaseline.Remove(project.Id);
                }
                catch (Exception ex) when (ex is PluginApplicationException or JsonException or HttpRequestException)
                {
                    projectsToRetry.Add(project.Id);
                    InvocationContext.Logger?.LogWarning($"Could not check workflow completion for project {project.Id}: {ex.Message}", []);
                }

                lastScannedProjectId = Math.Max(lastScannedProjectId ?? projectId, projectId);
            }
        }

        memory.LastScannedProjectId = scanDeferred ? lastScannedProjectId : null;
        memory.ProjectsToRetry = projectsToRetry.ToList();
        memory.ProjectsToBaseline = projectsToBaseline.ToList();
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
