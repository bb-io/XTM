using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using RestSharp;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Actions;

[ActionList]
public class ProjectActions : XtmInvocable
{
    public ProjectActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    #region Project actions

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
        
    [Action("Create project template", Description = "Create a new project template")]
    public Task<ProjectTemplate> CreateProjectTemplate([ActionParameter] CreateProjectTemplateRequest input)
    {
        if (input.TemplateType == "CUSTOMER" && input.CustomerId is null)
            throw new("You must specify customer ID for creating Customer templates");
        
        return Client.ExecuteXtmWithJson<ProjectTemplate>(ApiEndpoints.Templates,
            Method.Post,
            input,
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

    #endregion

    #region Project files actions

    [Action("Download source files as ZIP",
        Description = "Download the source files for project or specific jobs as ZIP")]
    public async Task<FileResponse> DownloadSourceFilesAsZip(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] [Display("Job IDs")] string[]? jobIds)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/download";

        if (jobIds != null)
            url += $"?{string.Join("&", jobIds.Select(x => $"jobIds={x}"))}";

        var response = await Client.ExecuteXtmWithJson(url,
            Method.Get,
            null,
            Creds);

        return new(new(response.RawBytes)
        {
            Name = $"Project-{project.ProjectId}SourceFiles.zip",
            ContentType = response.ContentType ?? MediaTypeNames.Application.Octet
        });
    }

    [Action("Download source files", Description = "Download the source files for project or specific jobs")]
    public async Task<SourceFilesResponse> DownloadSourceFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] [Display("Job IDs")] string[]? jobIds)
    {
        var zipFile = await DownloadSourceFilesAsZip(project, jobIds);

        var files = zipFile.File.Bytes.GetFilesFromZip();

        var result = new List<File>();
        await foreach (var file in files)
            result.Add(file);

        return new(result);
    }

    [Action("Download project file", Description = "Download a single, generated project file based on its ID")]
    public async Task<FileResponse> DownloadProjectFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] [Display("File ID")] string fileId)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/{fileId}/download?fileScope=JOB";

        var response = await Client.ExecuteXtmWithJson(url,
            Method.Get,
            null,
            Creds);

        return new(new(response.RawBytes)
        {
            Name = $"Project-{project.ProjectId}_File-{fileId}",
            ContentType = response.ContentType ?? MediaTypeNames.Application.Octet
        });
    }

    [Action("Upload source file", Description = "Upload source files for a project")]
    public async Task<CreateProjectResponse> UploadSourceFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UploadSourceFileRequest input)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/upload";
        var token = await Client.GetToken(Creds);

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + url,
            Method = Method.Post
        }, token);

        var parameters = new Dictionary<string, string>()
        {
            { "files[0].name", input.Name ?? input.File.Name },
            { "files[0].workflowId", input.WorkflowId },
            { "files[0].translationType", input.TranslationType },
        };

        if (input.Metadata != null)
        {
            parameters.Add("files[0].metadata", input.Metadata);
            parameters.Add("files[0].metadataType", input.MetadataType);
        }

        if (input.TagIds is not null)
        {
            var tags = input.TagIds.ToArray();
            for (var i = 0; i < tags.Length; i++)
                parameters.Add($"files[0].tagIds[{i}]", tags[i]);
        }

        if (input.TargetLanguages is not null)
        {
            var langs = input.TargetLanguages.ToArray();
            for (var i = 0; i < langs.Length; i++)
                parameters.Add($"files[0].targetLanguages[{i}]", langs[i]);
        }

        parameters.ToList().ForEach(x => request.AddParameter(x.Key, x.Value));
            
        request.AddFile("files[0].file", input.File.Bytes, input.Name ?? input.File.Name);
        request.AlwaysMultipartFormData = true;

        return await Client.ExecuteXtm<CreateProjectResponse>(request);
    }

    [Action("Upload translation file", Description = "Upload translation file to project")]
    public async Task<CreateProjectResponse> UploadTranslationFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UploadTranslationFileInput input)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/translations/upload";
        var token = await Client.GetToken(Creds);

        var parameters = new Dictionary<string, string>()
        {
            { "fileType", input.FileType },
            { "jobId", input.JobId },
            { "translationFile.name", input.Name ?? input.File.Name },
            { "xliffOptions.autopopulation", input.Autopopulation ? "ENABLED" : "DISABLED" },
            { "xliffOptions.segmentStatusApproving", input.SegmentStatusApproving },
        };

        if (!input.Autopopulation)
            parameters.Add("workflowStepName", input.WorkflowStepName);

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + url,
            Method = Method.Post
        }, token);

        parameters.ToList().ForEach(x => request.AddParameter(x.Key, x.Value, encode: false));
          
        request.AddFile("translationFile.file", input.File.Bytes, input.Name ?? input.File.Name);
        request.AlwaysMultipartFormData = true;

        return await Client.ExecuteXtm<CreateProjectResponse>(request);
    }

    #endregion
}