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
using System.ComponentModel.DataAnnotations;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Utils.Models;
using System.Text;
using Apps.XTM.Models.Response;
using Microsoft.Extensions.Options;

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
        [ActionParameter] GenerateFileRequest input)
    {
        var checkProject = Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{project.ProjectId}", Method.Get,null,Creds);
        
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/generate";

        var jobIds = new List<string>();

        if (input.jobIds == null)
        {
            var assignmentEndpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow/assignment";

            int page = 1;
            const int pageSize = 100;
            while (true)
            {
                var queryParams = new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "pageSize", pageSize.ToString() }
            };

                var assignmentResponse = await Client.ExecuteXtmWithJson<WorkflowAssignmentJobResponse>(
                    assignmentEndpoint.WithQuery(queryParams),
                    Method.Get,
                    null,
                    Creds
                );

                if (assignmentResponse.Jobs == null || assignmentResponse.Jobs.Count == 0)
                    break;

                jobIds.AddRange(assignmentResponse.Jobs.Select(j => j.JobId));

                page++;
            }

            input.jobIds = jobIds.ToArray();
        }

        var queryParameters = new Dictionary<string, string>
        {
            { "jobIds", string.Join(",", input.jobIds) },
            { "fileType", input.FileType }
        };

        if (input.TargetLanguage != null)
            queryParameters.Add("targetLanguage", input.TargetLanguage);

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + endpoint.WithQuery(queryParameters),
            Method = Method.Post
        }, await Client.GetToken(Creds));

        if (input.FileType is "HTML_EXTENDED_TABLE" or "PDF_EXTENDED_TABLE" or "EXCEL_EXTENDED_TABLE")
        {
            if (input.PropertiesToInclude is null || !input.PropertiesToInclude.Any(x => x.StartsWith("include")))
            {
                throw new PluginMisconfigurationException("Please specify the properties to include in the extended table file");
            }

            var tableType = "";
            switch (input.FileType)
            {
                case "HTML_EXTENDED_TABLE":
                    tableType = "htmlOptions";
                    break;
                case "PDF_EXTENDED_TABLE":
                    tableType = "pdfOptions";
                    break;
                case "EXCEL_EXTENDED_TABLE":
                    tableType = "excelOptions";
                    break;
            }

            var tableOptions = new Dictionary<string, string>();

            foreach (var key in input.PropertiesToInclude.Where(x => x.StartsWith("include")))
            {
                tableOptions[key] = "INCLUDE";
            }

            tableOptions["populateTargetWithSource"] = input.PropertiesToInclude.Contains("populateTargetWithSource") ? "POPULATE" : "DO_NOT_POPULATE";
            tableOptions["languagesType"] = input.TargetLanguage != null ? "SELECTED_LANGUAGES" : "ALL_LANGUAGES";
            tableOptions["extendedReportType"] = input.PropertiesToInclude.Contains("extendedReportType") ? "ALL_PROJECT_FILES_SINGLE_REPORT" :
                "ALL_PROJECT_FILES_MULTIPLE_REPORTS";


            var extendedTableOptions = new Dictionary<string, object>
            {
                [tableType] = tableOptions
            };

            var requestBody = new
            {
                extendedTableOptions = extendedTableOptions
            };

            request.AddJsonBody(requestBody);
        }

        try 
        {
            var response = await Client.ExecuteXtm<GeneratedFileResponse[]>(request);
            return new(response);
        } 
        catch (Exception e) 
        {
            if (e.Message.Contains("Request parameter seems to be invalid."))
            {
                throw new PluginMisconfigurationException("Please check that the inputs are correct. " + e.Message);
            }
            else 
            {
                throw new PluginApplicationException(e.Message);
            }
        }
        
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
        using var fileStream = new MemoryStream(response.RawBytes);
        var files = await fileStream.GetFilesFromZip();
        var xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmSourceFileDescription>>
            (response.Headers.First(header => header.Name == "xtm-file-descrption").Value.ToString());

        var result = new List<FileWithData<XtmSourceFileDescription>>();

        foreach (var file in files)
        {
            var uploadedFile =
                await _fileManagementClient.UploadAsync(file.FileStream, MediaTypeNames.Application.Octet,
                    file.UploadName);
            result.Add(new()
            {
                Content = uploadedFile,
                FileDescription =
                    xtmFileDescriptions?.FirstOrDefault(description => description.FileName == file.UploadName)
            });
        }

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

        using var fileStream = new MemoryStream(response.RawBytes);
        IEnumerable<BlackbirdZipEntry> files;
        try
        {
            files = await fileStream.GetFilesFromZip();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"The file returned from server is empty or damaged. Please check and try again. Message: {ex.Message}");
        }
        var file = files.FirstOrDefault();
        if (file == null)
        {
            throw new PluginApplicationException("No files found in the ZIP archive returned from the server.");
        }

        XtmProjectFileDescription xtmFileDescription;
        var header = response.Headers.FirstOrDefault(header => header.Name.Equals("xtm-file-descrption", StringComparison.OrdinalIgnoreCase));

        if (header != null)
        {
            try
            {
                xtmFileDescription = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>(
                    header.Value.ToString()).FirstOrDefault();
                if (xtmFileDescription != null)
                {
                    xtmFileDescription.FileName = file.UploadName;
                }
                else
                {
                    throw new PluginApplicationException("Failed to deserialize xtm-file-description header content.");
                }
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Error deserializing xtm-file-description header: {ex.Message}");
            }
        }
        else
        {
            var parts = file.UploadName.Split('_');
            if (parts.Length >= 3)
            {
                var targetLang = $"{parts[0]}_{parts[1]}";
                var fileName = string.Join("_", parts.Skip(2));
                xtmFileDescription = new XtmProjectFileDescription
                {
                    FileId = input.FileId,
                    FileName = fileName,
                    JobId = "",
                    TargetLanguage = targetLang
                };
            }
            else
            {
                xtmFileDescription = new XtmProjectFileDescription
                {
                    FileId = input.FileId,
                    FileName = file.UploadName,
                    JobId = "",
                    TargetLanguage = ""
                };
            }
        }

        var uploadedFile = await _fileManagementClient.UploadAsync(
            file.FileStream, MediaTypeNames.Application.Octet, file.UploadName);

        return new FileWithData<XtmProjectFileDescription>
        {
            Content = uploadedFile,
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

        using var fileStream = new MemoryStream(response.RawBytes);
        IEnumerable<BlackbirdZipEntry> files;
        try
        {
            files = await fileStream.GetFilesFromZip();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"The file returned from server is empty or damaged. Please check and try again. Message: {ex.Message}");
        }

        if (!files.Any())
        {
            throw new PluginApplicationException("No files found in the ZIP archive returned from the server.");
        }

        IEnumerable<XtmProjectFileDescription> xtmFileDescriptions = null;
        var header = response.Headers.FirstOrDefault(header => header.Name.Equals("xtm-file-descrption", StringComparison.OrdinalIgnoreCase));

        if (header != null)
        {
            try
            {
                xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>(header.Value.ToString());
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Error deserializing xtm-file-descrption header: {ex.Message}");
            }
        }

        var result = new List<FileWithData<XtmProjectFileDescription>>();
        foreach (var file in files)
        {
            var uploadedFile = await _fileManagementClient.UploadAsync(file.FileStream, MediaTypeNames.Application.Octet, file.UploadName);

            XtmProjectFileDescription description=null;
            if (xtmFileDescriptions != null)
            {
                var language = file.Path.Split('/').FirstOrDefault();
                var name = file.Path.Split('/').LastOrDefault();
                description = xtmFileDescriptions.FirstOrDefault(d => d.TargetLanguage == language && d.FileName == name);

                if (description != null)
                {
                    description.FileName = file.UploadName; 
                }
            }

            if (description == null)
            {
                var parts = file.UploadName.Split('_');
                if (parts.Length >= 3)
                {
                    var targetLang = $"{parts[0]}_{parts[1]}";
                    var fileName = string.Join("_", parts.Skip(2));
                    description = new XtmProjectFileDescription
                    {
                        FileId = "",
                        FileName = fileName,
                        JobId = "",
                        TargetLanguage = targetLang
                    };
                }
                else
                {
                    description = new XtmProjectFileDescription
                    {
                        FileId = "",
                        FileName = file.UploadName,
                        JobId = "",
                        TargetLanguage = ""
                    };
                }
            }

            result.Add(new FileWithData<XtmProjectFileDescription>
            {
                Content = uploadedFile,
                FileDescription = description
            });
        }

        return new DownloadFilesResponse<XtmProjectFileDescription>(result);
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

        using var fileStream = new MemoryStream(response.RawBytes);
        var files = await fileStream.GetFilesFromZip();

        IEnumerable<XtmProjectFileDescription> xtmFileDescriptions = null;
        var header = response.Headers.FirstOrDefault(h => h.Name == "xtm-file-descrption");
        if (header != null)
        {
            xtmFileDescriptions = JsonConvert.DeserializeObject<IEnumerable<XtmProjectFileDescription>>(header.Value.ToString());
        }

        var result = new List<FileWithData<XtmProjectFileDescription>>();

        foreach (var file in files)
        {
            var uploadedFile = await _fileManagementClient.UploadAsync(file.FileStream, MediaTypeNames.Application.Octet, file.UploadName);

            var description = xtmFileDescriptions?.FirstOrDefault(d => (d.TargetLanguage + "_" + d.FileName) == file.UploadName);

            if (description == null)
            {
                var parts = file.UploadName.Split('_');
                if (parts.Length >= 3)
                {
                    var targetLang = $"{parts[0]}_{parts[1]}";
                    var fileName = string.Join("_", parts.Skip(2));
                    description = new XtmProjectFileDescription
                    {
                        FileId = "",
                        FileName = fileName,
                        JobId = "", 
                        TargetLanguage = targetLang
                    };
                }
                else
                {
                    description = new XtmProjectFileDescription
                    {
                        FileId = "",
                        FileName = file.UploadName,
                        JobId = "",
                        TargetLanguage = ""
                    };
                }
            }

            result.Add(new FileWithData<XtmProjectFileDescription>
            {
                Content = uploadedFile,
                FileDescription = description
            });
        }

        return new DownloadFilesResponse<XtmProjectFileDescription>(result);
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

        if (!string.IsNullOrWhiteSpace(input.ReanalyseProject)
            && (input.ReanalyseProject =="YES" || input.ReanalyseProject=="NO"))
        {
            parameters.Add("reanalyseProject", input.ReanalyseProject);
        }

        if (input.TargetLanguages is not null)
        {
            var langs = input.TargetLanguages.ToArray();
            for (var i = 0; i < langs.Length; i++)
                parameters.Add($"files[0].targetLanguages[{i}]", langs[i]);
        }

        parameters.ToList().ForEach(x => request.AddParameter(x.Key, x.Value));

        string fileName = input.Name ?? input.File.Name ?? throw new PluginMisconfigurationException("File name is required");
       

        var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("files[0].file", fileBytes, fileName);
        request.AlwaysMultipartFormData = true;

        try 
        {
            return await Client.ExecuteXtm<CreateProjectResponse>(request);
        } 
        catch (Exception e)
        {
            if (e.Message.Contains("Please wait for analysis"))
            {
                throw new PluginMisconfigurationException("File cannot be uploaded because the project is under analysis. " +
                    "Consider using a Checkpoint, set the reanalize project optional input to false or adding retries in the error handling tab");
            }
            else
            {
                throw new PluginApplicationException(e.Message);
            }
        }
       
    }

    [Action("Upload translation file", Description = "Upload translation file to project")]
    public async Task<UploadTranslationFileResponse> UploadTranslationFile(
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

        try
        {
            var response = await Client.ExecuteXtm<FileUploadResponse>(request);

            var uploadStatusResponse = await PollFileStatusAsync(project.ProjectId, response.File.FileId, input.FileType);
            return new()
            {
                FileId = response.File.FileId,
                JobId = response.File.JobId,
                Status = uploadStatusResponse.Status
            };
        }
        catch (Exception ex)
        {
           throw new PluginApplicationException(ex.Message);
            
        }
       
    }

    private async Task<UploadStatusResponse> PollFileStatusAsync(string projectId, string fileId, string fileType)
    {
        var statusUrl = $"{ApiEndpoints.Projects}/{projectId}/files/translations/{fileId}/status?fileType={fileType}";
        UploadStatusResponse uploadStatusResponse;

        do
        {
            uploadStatusResponse = await Client.ExecuteXtmWithJson<UploadStatusResponse>(
                statusUrl,
                Method.Get,
                null,
                Creds
            );

            if (uploadStatusResponse.Status == "ERROR")
            {
                throw new Exception(
                    $"Failed to upload translation file. Status: {uploadStatusResponse.Status}, Error description: {uploadStatusResponse.ErrorDescription}");
            }

            if (uploadStatusResponse.Status != "FINISHED")
            {
                await Task.Delay(5000);
            }
        } while (uploadStatusResponse.Status != "FINISHED");

        return uploadStatusResponse;
    }
}