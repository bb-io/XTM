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
    [Action("List workflows", Description = "List all workflows")]
    public async Task<AllWorkflowsResponse> ListWorkflows()
    {
        var response = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}",
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("List workflow steps", Description = "List all workflow steps")]
    public async Task<AllWorkflowStepsResponse> ListWorkflowSteps()
    {
        var endpoint = $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}";
        var response = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(endpoint,
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("Get workflow by ID", Description = "Get workflow by ID")]
    public async Task<WorkflowResponse> GetWorkflow([ActionParameter, Display("Workflow ID")] string workflowId)
    {
        var workflows = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}?ids={workflowId}",
                         Method.Get,
                         null,
                         Creds);

        if(workflows == null || !workflows.Any())
        {
            throw new Exception($"Workflow with ID {workflowId} not found");
        }

        return workflows.First();
    }

    [Action("Assign to workflow", Description ="Assining users to workflow ")]
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
        var response = await Client.ExecuteXtmWithJson<WorkflowAssignmentResponse>(endpoint,Method.Post,requestBody,Creds);

        return response;
    }

    [Action("Move jobs to next workflow step", Description = "Moves jobs to the next workflow step in the project")]
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

    [Action("Start workflow in project", Description = "Activate the first workflow step for all jobs in the project")]
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

        var response = await Client.ExecuteXtmWithJson<StartWorkflowResponse>(endpoint,Method.Post,queryParams,Creds);

        return response;
    }
}