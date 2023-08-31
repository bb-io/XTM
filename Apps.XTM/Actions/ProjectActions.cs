using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class ProjectActions : XtmInvocable
{
    public ProjectActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List projects", Description = "List all projects")]
    public async Task<ListProjectsResponse> ListProjects()
    {
        var response = await Client.ExecuteXtmWithJson<List<SimpleProject>>(ApiEndpoints.Projects,
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    [Action("Get project", Description = "Get project by ID")]
    public Task<FullProject> GetProject([ActionParameter] ProjectRequest project)
    {
        return Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{project.ProjectId}",
            Method.Get,
            null,
            Creds);
    }

    [Action("Create project", Description = "Create new project")]
    public Task<CreateProjectResponse> CreateProject([ActionParameter] CreateProjectRequest input)
    {
        var parameters = new Dictionary<string, string>()
        {
            { "name", input.Name },
            { "description", input.Description },
            { "customerId", input.CustomerId },
            { "workflowId", input.WorkflowId },
            { "sourceLanguage", input.SourceLanguge },
            { "targetLanguages", input.TargetLanguage },
            { "callbacks.projectCreatedCallback", input.ProjectCreatedCallback },
            { "callbacks.projectAcceptedCallback", input.ProjectAcceptedCallback },
            { "callbacks.projectFinishedCallback", input.ProjectFinishedCallback },
            { "callbacks.jobFinishedCallback", input.JobFinishedCallback },
            { "callbacks.analysisFinishedCallback", input.AnalysisFinishedCallback },
            { "callbacks.workflowTransitionCallback", input.WorkflowTransitionCallback },
            { "callbacks.invoiceStatusChangedCallback", input.InvoiceStatusChangedCallback },
        };

        return Client.ExecuteXtmWithFormData<CreateProjectResponse>(ApiEndpoints.Projects,
            Method.Post,
            parameters,
            Creds);
    }

    [Action("Create new project from template",
        Description = "Create a new project using an existing project template")]
    public Task<CreateProjectResponse> CreateProjectFromTemplate(
        [ActionParameter] CreateProjectFromTemplateRequest input)
    {
        var parameters = new Dictionary<string, string>()
        {
            { "name", input.Name },
            { "description", input.Description },
            { "customerId", input.CustomerId },
            { "templateId", input.TemplateId }
        };

        return Client.ExecuteXtmWithFormData<CreateProjectResponse>(ApiEndpoints.Projects,
            Method.Post,
            parameters,
            Creds);
    }

    [Action("Clone project", Description = "Create a new project based on the provided project")]
    public Task<CreateProjectResponse> CloneProject([ActionParameter] CloneProjectRequest input)
    {
        var parameters = new Dictionary<string, string>()
        {
            { "name", input.Name },
            { "originId", input.OriginId },
        };

        return Client.ExecuteXtmWithFormData<CreateProjectResponse>($"{ApiEndpoints.Projects}/clone",
            Method.Post,
            parameters,
            Creds);
    }

    [Action("Update project", Description = "Update specific project")]
    public Task<ManageEntityResponse> UpdateProject(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UpdateProjectRequest input)
    {
        return Client.ExecuteXtmWithJson<ManageEntityResponse>($"{ApiEndpoints.Projects}/{project.ProjectId}",
            Method.Put,
            input,
            Creds);
    }
        
    [Action("Add project target languages", Description = "Add more target languages to a specific project")]
    public Task<CreateProjectResponse> AddTargetLanguages(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] TargetLanguagesRequest input)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/target-languages";
        return Client.ExecuteXtmWithJson<CreateProjectResponse>(endpoint,
            Method.Post,
            input,
            Creds);
    }
        
    [Action("Delete project target languages", Description = "Delete specific target languages from a project")]
    public Task<ManageEntityResponse> DeleteTargetLanguages(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] TargetLanguagesRequest input)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/target-languages";
        return Client.ExecuteXtmWithJson<ManageEntityResponse>(endpoint,
            Method.Delete,
            input,
            Creds);
    }     
        
    [Action("Reanalyze project", Description = "Reanalyze specific project")]
    public Task<ManageEntityResponse> ReanalyzeProject(
        [ActionParameter] ProjectRequest project)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/reanalyze";
        return Client.ExecuteXtmWithJson<ManageEntityResponse>(endpoint,
            Method.Post,
            null,
            Creds);
    }

    [Action("Delete project", Description = "Delete specific project")]
    public Task DeleteProject([ActionParameter] ProjectRequest project)
    {
        return Client.ExecuteXtmWithJson($"{ApiEndpoints.Projects}/{project.ProjectId}",
            Method.Delete,
            null,
            Creds);
    }
        
    [Action("Get project estimates", Description = "Get specific project estimates")]
    public Task<ProjectEstimates> GetProjectEstimates([ActionParameter] ProjectRequest project)
    {
        return Client.ExecuteXtmWithJson<ProjectEstimates>($"{ApiEndpoints.Projects}/{project.ProjectId}/proposal",
            Method.Get,
            null,
            Creds);
    }
}