﻿using System.Net.Mime;
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
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class FileActions : XtmInvocable
{
    private readonly IFileManagementClient _fileManagementClient;

    public FileActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
        : base(invocationContext)
    {
        _fileManagementClient = fileManagementClient;
    }

    [Action("Generate files", Description = "Generate project files")]
    public async Task<ListGeneratedFilesResponse> GenerateFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] GenerateFileRequest query)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/generate";

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + endpoint.WithQuery(query),
            Method = Method.Post
        }, await Client.GetToken(Creds));

        var response = await Client.ExecuteXtm<GeneratedFileResponse[]>(request);
        return new(response);
    }

    [Action("Download source files as ZIP",
        Description = "Download the source files for project or specific jobs as ZIP")]
    public async Task<FileResponse> DownloadSourceFilesAsZip(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] JobsRequest jobs)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/download";

        if (jobs.JobIds != null)
            url += $"?{string.Join("&", jobs.JobIds.Select(x => $"jobIds={x}"))}";

        var response = await Client.ExecuteXtmWithJson(url,
            Method.Get,
            null,
            Creds);

        using var stream = new MemoryStream(response.RawBytes);
        var file = await _fileManagementClient.UploadAsync(stream,
            response.ContentType ?? MediaTypeNames.Application.Octet, $"Project-{project.ProjectId}SourceFiles.zip");

        return new(file);
    }

    [Action("Download source files", Description = "Download the source files for project or specific jobs")]
    public async Task<DownloadFilesResponse<XtmSourceFileDescription>> DownloadSourceFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] JobsRequest jobs)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/download";

        if (jobs.JobIds != null)
            url += $"?{string.Join("&", jobs.JobIds.Select(x => $"jobIds={x}"))}";

        var response = await Client.ExecuteXtmWithJson(url,
            Method.Get,
            null,
            Creds);

        var files = await response.RawBytes.GetFilesFromZip(_fileManagementClient);
        var xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmSourceFileDescription>>
            (response.Headers.First(header => header.Name == "xtm-file-descrption").Value.ToString());

        var result = new List<FileWithData<XtmSourceFileDescription>>();

        foreach (var file in files)
            result.Add(new()
            {
                Content = file.File,
                FileDescription = xtmFileDescriptions.First(description => description.FileName == file.File.Name)
            });

        return new(result);
    }

    [Action("Download project file", Description = "Download a single, generated project file based on its ID")]
    public async Task<FileWithData<XtmProjectFileDescription>> DownloadProjectFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadProjectFileRequest input)
    {
        var url =
            $"{ApiEndpoints.Projects}/{project.ProjectId}/files/{input.FileId}/download?fileScope={input.FileScope}";

        var response = await Client.ExecuteXtmWithJson(url,
            Method.Get,
            null,
            Creds);

        var file = (await response.RawBytes.GetFilesFromZip(_fileManagementClient)).First();
        var xtmFileDescription = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>
            (response.Headers.First(header => header.Name == "xtm-file-descrption").Value.ToString()).First();
        xtmFileDescription.FileName = file.File.Name;

        return new()
        {
            Content = file.File,
            FileDescription = xtmFileDescription
        };
    }

    [Action("Download all project files", Description = "Download all of the project files")]
    public async Task<DownloadFilesResponse<XtmProjectFileDescription>> DownloadProjectFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadAllProjectFilesRequest input)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/download";

        var queryParameters = new Dictionary<string, string>
        {
            { "fileScope", input.FileScope },
            { "fileType", input.FileType }
        };

        if (input.JobIds != null)
            queryParameters.Add("jobIds", string.Join(",", input.JobIds));

        if (input.TargetLanguages != null)
            queryParameters.Add("targetLanguages", string.Join(",", input.TargetLanguages));

        var response = await Client.ExecuteXtmWithJson(url.WithQuery(queryParameters),
            Method.Get,
            null,
            Creds);

        var files = await response.RawBytes.GetFilesFromZip(_fileManagementClient);
        var xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>
            (response.Headers.First(header => header.Name == "xtm-file-descrption").Value.ToString());

        var result = new List<FileWithData<XtmProjectFileDescription>>();
        foreach (var file in files)
        {
            result.Add(new()
            {
                Content = file.File,
                FileDescription = xtmFileDescriptions.First(description => description.FileName == file.File.Name)
            });
        }

        return new(result);
    }

    [Action("Download translated files", Description = "Download project's translated files")]
    public async Task<DownloadFilesResponse<XtmProjectFileDescription>> DownloadTranslations(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadTranslationsRequest input)
    {
        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/download";

        var queryParameters = new Dictionary<string, string>
        {
            { "fileScope", "JOB" },
            { "fileType", "TARGET" }
        };

        if (input.JobIds != null)
            queryParameters.Add("jobIds", string.Join(",", input.JobIds));

        if (input.TargetLanguages != null)
            queryParameters.Add("targetLanguages", string.Join(",", input.TargetLanguages));

        var response = await Client.ExecuteXtmWithJson(url.WithQuery(queryParameters),
            Method.Get,
            null,
            Creds);

        var files = await response.RawBytes.GetFilesFromZip(_fileManagementClient);
        var xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>
            (response.Headers.First(header => header.Name == "xtm-file-descrption").Value.ToString());

        var result = new List<FileWithData<XtmProjectFileDescription>>();
        foreach (var file in files)
        {
            result.Add(new()
            {
                Content = file.File,
                FileDescription = xtmFileDescriptions.First(description => description.FileName == file.File.Name)
            });
        }

        return new(result);
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

        var parameters = new Dictionary<string, string>
        {
            { "files[0].name", input.Name?.Trim() ?? input.File.Name },
            { "files[0].workflowId", input.WorkflowId }
        };

        if (input.TranslationType != null)
            parameters.Add("files[0].translationType", input.TranslationType);

        if (input.Metadata != null)
        {
            parameters.Add("files[0].metadata", input.Metadata);
            parameters.Add("files[0].metadataType", "JSON"); // JSON is the only available type of metadata
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

        var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("files[0].file", fileBytes, input.Name ?? input.File.Name);
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

        var parameters = new Dictionary<string, string>
        {
            { "fileType", input.FileType },
            { "jobId", input.JobId },
            { "translationFile.name", input.Name?.Trim() ?? input.File.Name },
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

        var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("translationFile.file", fileBytes, input.Name ?? input.File.Name);
        request.AlwaysMultipartFormData = true;

        return await Client.ExecuteXtm<CreateProjectResponse>(request);
    }
}