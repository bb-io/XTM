using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using RestSharp;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Actions;

[ActionList]
public class FileActions : XtmInvocable
{
    public FileActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
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
}