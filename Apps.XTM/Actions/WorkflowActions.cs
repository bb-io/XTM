using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList("Workflows")]
public class WorkflowActions(InvocationContext invocationContext) : XtmInvocable(invocationContext)
{
    [Action("Update due dates", Description = "Set a project, workflow, step, job, or target language date")]
    public async Task<UpdateDueDatesResponse> UpdateDueDates(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UpdateDueDatesRequest input)
    {
        if (!long.TryParse(project.ProjectId, out var projectId) || projectId <= 0)
            throw new PluginMisconfigurationException("Project ID must be a positive integer.");

        if (input.Date == default)
            throw new PluginMisconfigurationException("Date is required.");

        var requiresTarget = input.Type is "steps" or "jobs" or "targetLanguages";
        if (!requiresTarget && input.Type is not ("projectDueDate" or "newSourceDueDate" or "workflowStartDate" or "workflowDueDate"))
            throw new PluginMisconfigurationException("Select a valid date type.");

        var target = input.TargetIdentifier?.Trim();
        if (requiresTarget && string.IsNullOrEmpty(target))
            throw new PluginMisconfigurationException("Target identifier is required for step, job, and target language dates.");
        if (!requiresTarget && !string.IsNullOrEmpty(target))
            throw new PluginMisconfigurationException("Leave Target identifier empty for project and workflow dates.");

        long jobId = 0;
        if (input.Type == "jobs" && (!long.TryParse(target, out jobId) || jobId <= 0))
            throw new PluginMisconfigurationException("Job ID must be a positive integer.");

        var utcDate = input.Date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(input.Date, DateTimeKind.Utc)
            : input.Date.ToUniversalTime();
        var date = utcDate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        object value = input.Type switch
        {
            "steps" => new[] { new { referenceStepName = target, dueDate = date } },
            "jobs" => new[] { new { id = jobId, dueDate = date } },
            "targetLanguages" => new[] { new { languageCode = target, dueDate = date } },
            _ => date
        };

        return await Client.ExecuteXtmWithJson<UpdateDueDatesResponse>(
            $"{ApiEndpoints.Projects}/{projectId}/due-dates", Method.Put,
            new Dictionary<string, object> { [input.Type] = value }, Creds);
    }

    [Action("Search workflows", Description = "Search workflows")]
    public async Task<AllWorkflowsResponse> ListWorkflows()
    {
        var response = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}",
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("Search workflow steps", Description = "Search workflow steps")]
    public async Task<AllWorkflowStepsResponse> ListWorkflowSteps()
    {
        var endpoint = $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}";
        var response = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(endpoint,
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("Get workflow by ID", Description = "Get workflow details by ID")]
    public async Task<WorkflowResponse> GetWorkflow([ActionParameter, Display("Workflow ID")] string workflowId)
    {
        var workflows = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}?ids={workflowId}",
                         Method.Get,
                         null,
                         Creds);

        if (workflows == null || !workflows.Any())
        {
            throw new PluginApplicationException($"Workflow with ID {workflowId} not found");
        }

        return workflows.First();
    }

    [Action("Assign to workflow", Description = "Assign users to workflow steps in a project")]
    public async Task<WorkflowAssignmentResponse> AssignUserToWorkflow([ActionParameter] WorkflowAssignmentRequest assignmentRequest,
        [ActionParameter] ProjectRequest inputProject)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{inputProject.ProjectId}/workflow/assign";

        var requestBody = new[]
    {
        new
        {
            user = new
            {
                id = int.TryParse(assignmentRequest.UserId, out var parsedId) ? parsedId : throw new PluginMisconfigurationException("Invalid User ID"),
                type = assignmentRequest.UserType ?? "INTERNAL_USER"
            },
            languages = assignmentRequest.Languages ?? new List<string>(),
            stepNames = assignmentRequest.StepName ?? new List<string>(),
            jobIds = assignmentRequest.JobIds ?? new List<string>(),
            bundleIds = assignmentRequest.BundleIds ?? new List<string>()
        }
    };
        var response = await Client.ExecuteXtmWithJson<WorkflowAssignmentResponse>(endpoint, Method.Post, requestBody, Creds);

        return response;
    }

    [Action("Move jobs to next workflow step", Description = "Move jobs to the next workflow step in a project")]
    public async Task<MoveJobsToNextStepResponse> MoveJobsToNextWorkflowStep(
        [ActionParameter] ProjectRequest inputProject,
        [ActionParameter] MailingRequest inputMail,
        [ActionParameter] MoveJobsToNextStepRequest inputMove)
    {
        var token = await Client.GetToken(Creds);

        var targetJobIds = inputMove.JobIds;
        if (!string.IsNullOrEmpty(inputMove.CurrentWorkflowStep))
        {
            var projectStatusEndpoint = $"{ApiEndpoints.Projects}/{inputProject.ProjectId}/status?fetchLevel=STEPS";

            var projectStatusRequest = new XTMRequest(new()
            {
                Url = Creds.Get(CredsNames.Url) + projectStatusEndpoint,
                Method = Method.Get,
            }, await Client.GetToken(Creds));

            var projectDetailedStatusResponse = await Client.ExecuteXtm<ProjectDetailedStatusResponse>(projectStatusRequest);

            targetJobIds = projectDetailedStatusResponse?.Jobs?
                .Where(job => targetJobIds.Contains(job.JobId))
                .Where(job =>
                {
                    var step = job.Steps?.FirstOrDefault(s =>
                        s.WorkflowStepName.Equals(inputMove.CurrentWorkflowStep, StringComparison.OrdinalIgnoreCase));

                    return step != null && (step.Status is "IN_PROGRESS" or "NOT_STARTED");
                })
                .Select(job => job.JobId)
                .ToList() ?? [];
        }

        if (targetJobIds.Count == 0)
            return new();

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + $"{ApiEndpoints.Projects}/{inputProject.ProjectId}/workflow/finish",
            Method = Method.Post
        }, token);

        request.AddQueryParameter("jobIds", string.Join(",", targetJobIds));
        var mailing = inputMail.Mailing ?? "DISABLED";
        request.AddQueryParameter("mailing", mailing);

        var response = await Client.ExecuteXtm<MoveJobsToNextStepResponse>(request);
        return response;
    }

    [Action("Start workflow in project", Description = "Start workflow steps for jobs in a project")]
    public async Task<StartWorkflowResponse> StartWorkflowInProject([ActionParameter] WorklowLanguagesRequest inputLanguage,
        [ActionParameter] ProjectRequest inputProject,
        [ActionParameter] JobsRequest inputJob)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{inputProject.ProjectId}/workflow/start";

        var queryParams = new
        {
            jobIds = inputJob.JobIds,
            targetLanguages = inputLanguage.TargetLanguages
        };

        var response = await Client.ExecuteXtmWithJson<StartWorkflowResponse>(endpoint, Method.Post, queryParams, Creds);

        return response;
    }
}
