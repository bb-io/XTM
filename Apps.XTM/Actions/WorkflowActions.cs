using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Jobs;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.Workflows;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using RestSharp;
using static System.Net.WebRequestMethods;

namespace Apps.XTM.Actions;

[ActionList]
public class WorkflowActions : XtmInvocable
{
    public WorkflowActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    #region Actions

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
                id = int.TryParse(assignmentRequest.UserId, out var parsedId) ? parsedId : throw new Exception("Invalid User ID"),
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
    public async Task<MoveJobsToNextStepResponse> MoveJobsToNextWorkflowStep([ActionParameter] JobsRequest inputJobs,
        [ActionParameter] ProjectRequest inputProject,
        [ActionParameter] MailingRequest inputMail)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{inputProject.ProjectId}/workflow/finish";

        var queryParams = new
        {
            jobIds = inputJobs.JobIds,
            mailing = inputMail.Mailing ?? "ENABLED"
        };
        try 
        {
            var response = await Client.ExecuteXtmWithJson<MoveJobsToNextStepResponse>(
            endpoint,
            Method.Post,
            queryParams,
            Creds);
            return response;
        } catch (Exception ex) 
        {
            if (ex.Message.Contains("check the state of workflow")) 
            { throw new PluginMisconfigurationException(ex.Message); } 
            else 
            {
                throw new PluginApplicationException(ex.Message);
            }
        }
        
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

    #endregion
}