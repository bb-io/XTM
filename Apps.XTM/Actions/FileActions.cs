using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.Models;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using Microsoft.AspNetCore.WebUtilities;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using System.Text;
using System.Xml.Linq;

namespace Apps.XTM.Actions;

[ActionList]
public class FileActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : XtmInvocable(invocationContext)
{
    private readonly IFileManagementClient _fileManagementClient = fileManagementClient;

    [Action("Generate files", Description = "Generate project files")]
    public async Task<ListGeneratedFilesResponse> GenerateFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] GenerateFileRequest input)
    {
        if (input.JobIds?.Any() == true && input.ActiveWorkflowSteps?.Any() == true)
            throw new PluginMisconfigurationException("Please specify either Job IDs or active workflow steps, not both, as action can filter either by Job IDs or active workflow step.");

        if (input.JobIds?.Any() != true)
        {
            var projectStatusEndpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/status";
            var queryParams = new Dictionary<string, string?>();

            if (input.ActiveWorkflowSteps?.Any() != true)
                queryParams.Add("fetchLevel", "JOBS");
            else
            {
                queryParams.Add("fetchLevel", "STEPS");
                queryParams.Add("stepReferenceNames", string.Join(",", input.ActiveWorkflowSteps));
            }

            var projectStatusRequest = new XTMRequest(new()
            {
                Url = Creds.Get(CredsNames.Url) + QueryHelpers.AddQueryString(projectStatusEndpoint, queryParams),
                Method = Method.Get,
            }, await Client.GetToken(Creds));

            var projectDetailedStatusResponse = await Client.ExecuteXtm<ProjectDetailedStatusResponse>(projectStatusRequest);
            var jobs = projectDetailedStatusResponse.Jobs.AsEnumerable();

            if (input.ActiveWorkflowSteps?.Any() == true)
            {
                jobs = jobs.Where(j => j.Steps.Count > 0 && j.Steps.All(s => s.Status == "IN_PROGRESS"));

                if (!jobs.Any())
                    return new ListGeneratedFilesResponse([]);
            }

            input.JobIds = jobs.Select(j => j.JobId).ToList();
        }

        var generateEndpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/generate";
        var queryParameters = new Dictionary<string, string>
        {
            { "jobIds", string.Join(",", input.JobIds) },
            { "fileType", input.FileType }
        };

        if (input.TargetLanguage != null)
            queryParameters.Add("targetLanguage", input.TargetLanguage);

        var requestParameters = new XtmRequestParameters()
        {
            Url = Creds.Get(CredsNames.Url) + generateEndpoint.WithQuery(queryParameters),
            Method = Method.Post,
        };

        var request = new XTMRequest(requestParameters, await Client.GetToken(Creds));

        if (input.FileType is "HTML_EXTENDED_TABLE" or "PDF_EXTENDED_TABLE" or "EXCEL_EXTENDED_TABLE")
        {
            if (input.PropertiesToInclude is null || !input.PropertiesToInclude.Any(x => x.StartsWith("include")))
                throw new PluginMisconfigurationException("Please specify the properties to include in the extended table file");

            var tableType = input.FileType switch
            {
                "HTML_EXTENDED_TABLE" => "htmlOptions",
                "PDF_EXTENDED_TABLE" => "pdfOptions",
                "EXCEL_EXTENDED_TABLE" => "excelOptions",
                _ => string.Empty
            };

            var tableOptions = new Dictionary<string, string>();

            foreach (var key in input.PropertiesToInclude.Where(x => x.StartsWith("include")))
                tableOptions[key] = "INCLUDE";

            tableOptions["populateTargetWithSource"] = input.PropertiesToInclude.Contains("populateTargetWithSource") ? "POPULATE" : "DO_NOT_POPULATE";
            tableOptions["languagesType"] = input.TargetLanguage != null ? "SELECTED_LANGUAGES" : "ALL_LANGUAGES";
            tableOptions["extendedReportType"] = input.PropertiesToInclude.Contains("extendedReportType") ? "ALL_PROJECT_FILES_SINGLE_REPORT" : "ALL_PROJECT_FILES_MULTIPLE_REPORTS";

            request.AddJsonBody(new
            {
                extendedTableOptions = new Dictionary<string, object> { [tableType] = tableOptions },
            });
        }

        try 
        {
            var response = await Client.ExecuteXtm<GeneratedFileResponse[]>(request);
            return new(response);
        }
        catch (Exception ex) 
        {
            if (ex.Message.Contains("Request parameter seems to be invalid."))
                throw new PluginMisconfigurationException("Please check that the inputs are correct. " + ex.Message);
            else 
                throw new PluginApplicationException(ex.Message);
        }
    }



    [Action("Download source files as ZIP",
        Description = "Download the source files for project or specific jobs as ZIP")]
    public async Task<FileResponse> DownloadSourceFilesAsZip(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] JobsRequest jobs)
    {
        var zip = await DownloadSourceFilesZip(project.ProjectId, jobs.JobIds);
        using var stream = new MemoryStream(zip);

        var fileName = $"Project-{project.ProjectId}-SourceFiles.zip";
        var file = await _fileManagementClient.UploadAsync(stream, MimeTypes.GetMimeType(fileName), fileName);

        return new(file);
    }

    [Action("Download source files", Description = "Download the source files for project or specific jobs")]
    public async Task<DownloadFilesResponse<XtmSourceFileDescription>> DownloadSourceFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] JobsRequest jobsRequest)
    {
        // XTM API won't reliably return file info for 50+ files,
        // so instead of asking for all files at once
        // we will fetch them in batches of 50
        var projectJobLevelStatusJobResponse = await Client.ExecuteXtmWithJson<ProjectJobLevelStatusDto>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Status}?fetchLevel=JOBS",
            Method.Get,
            null,
            Creds);

        var jobStatusByFileName = projectJobLevelStatusJobResponse
            .Jobs
            .Where(j => !string.Equals(j.CompletionStatus, "DELETED", StringComparison.OrdinalIgnoreCase))
            .Where(j => jobsRequest.JobIds?.Count() > 0 ? jobsRequest.JobIds.Contains(j.JobId.ToString()) : true)
            .ToLookup(j => j.FileName);

        var sourceFilesWithFirstActiveJob = jobStatusByFileName
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());

        var sourceFiles = new List<FileWithData<XtmSourceFileDescription>>();

        foreach (var batch in sourceFilesWithFirstActiveJob.Batch(20))
        {
            // When passing multiple Job IDs per source file
            // XTM API won't return more than one job per source file
            // and will change filenames by adding a job ID like this: "filename(jobId).extention"
            // so we have to get one job ID per source file
            // and then rebuild the file description ourselves
            var jobIds = batch
                .Select(j => j.Value?.JobId.ToString() ?? string.Empty)
                .Where(jobId => !string.IsNullOrWhiteSpace(jobId));

            var zip = await DownloadSourceFilesZip(project.ProjectId, jobIds);

            using var fileStream = new MemoryStream(zip ?? []);
            var files = await fileStream.GetFilesFromZip();

            foreach (var file in files)
            {
                var fileReference = await _fileManagementClient.UploadAsync(
                        file.FileStream,
                        MimeTypes.GetMimeType(file.UploadName),
                        file.UploadName);

                var fileDescription = new XtmSourceFileDescription
                {
                    FileId = jobStatusByFileName[file.UploadName].FirstOrDefault()?.SourceFileId.ToString() ?? string.Empty,
                    FileName = file.UploadName,
                    JobIds = jobStatusByFileName[file.UploadName]
                        .Where(j => j.FileName == file.UploadName)
                        .Select(j => j.JobId.ToString())
                };

                sourceFiles.Add(new()
                {
                    Content = fileReference,
                    FileDescription = fileDescription,
                });
            }
        }

        return new(sourceFiles);
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
            file.FileStream, MimeTypes.GetMimeType(file.UploadName), file.UploadName);

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
            var uploadedFile = await _fileManagementClient.UploadAsync(file.FileStream, MimeTypes.GetMimeType(file.UploadName), file.UploadName);

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
            var uploadedFile = await _fileManagementClient.UploadAsync(file.FileStream, MimeTypes.GetMimeType(file.UploadName), file.UploadName);

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
            request.AddQueryParameter("reanalyseProject", input.ReanalyseProject);
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

    [Action("Upload reference file",
    Description = "Upload a reference file to an XTM project")]
    public async Task UploadReferenceFile(
    [ActionParameter] ProjectRequest project,
    [ActionParameter] UploadReferenceFileRequest input)
    {
        if (input.File == null)
            throw new PluginMisconfigurationException("Reference file is required.");

        var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/reference-materials/upload";
        var token = await Client.GetToken(Creds);

        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + url,
            Method = Method.Post
        }, token);

        string fileName = input.Name?.Trim() ?? input.File.Name;
        request.AddParameter("referenceMaterialsFiles[0].name", fileName);
        var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();

        request.AddFile("referenceMaterialsFiles[0].file", fileBytes, fileName);

        request.AlwaysMultipartFormData = true;

        try
        {
            await Client.ExecuteXtm<object>(request);
        }
        catch (Exception e)
        {
            throw new PluginApplicationException(e.Message);
        }
    }

    [Action("Upload translation file", Description = "Upload translation file to project")]
    public async Task<UploadTranslationFileResponse> UploadTranslationFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UploadTranslationFileRequest input,
        [ActionParameter] UploadTranslationFileEstimatesRequest estimatesRequest)
    {
        var request = new XTMRequest(new()
        {
            Url = Creds.Get(CredsNames.Url) + $"{ApiEndpoints.Projects}/{project.ProjectId}/files/translations/upload",
            Method = Method.Post,
        }, await Client.GetToken(Creds));

        var parameters = new Dictionary<string, string>
        {
            { "fileType", input.FileType },
            { "jobId", input.JobId },
            { "translationFile.name", input.Name?.Trim() ?? input.File.Name },
            { "xliffOptions.autopopulation", input.Autopopulation != false ? "ENABLED" : "DISABLED" },
            { "xliffOptions.segmentStatusApproving", input.SegmentStatusApproving ?? "ACCORDINGLY_TO_STATE" },
        };

        if (input.Autopopulation == false && !string.IsNullOrEmpty(input.WorkflowStepName))
            parameters.Add("workflowStepName", input.WorkflowStepName);

        parameters.ToList().ForEach(x => request.AddParameter(x.Key, x.Value, encode: false));

        var inputFileStream = await _fileManagementClient.DownloadAsync(input.File);
        byte[] fileBytes = [];

        if (estimatesRequest.LockSegmentsAboveThreshold == true
            || estimatesRequest.MarkSegmentsUnderThresholdAsNotCompleted == true)
        {
            var transformation = await Transformation.Parse(inputFileStream, input.File.Name);
            var units = transformation.GetUnits()
                .Where(u => u.Quality.Score != null && u.Quality.ScoreThreshold != null);

            if (!units.Any())
                throw new PluginMisconfigurationException("The provided file does not contain any quality score and threshold pairs.");

            var xtmNamespace = XNamespace.Get("urn:xliff-xtm-extensions");
            var lockedAttribute = new XAttribute(xtmNamespace + "locked", "yes");

            foreach (var unit in units)
            {
                if (estimatesRequest.MarkSegmentsUnderThresholdAsNotCompleted == true)
                {
                    foreach (var segment in unit.Segments)
                    {
                        if (unit.Quality.Score < unit.Quality.ScoreThreshold)
                            segment.State = null;
                    }
                }

                if (estimatesRequest.LockSegmentsAboveThreshold == true
                    && unit.Quality.Score >= unit.Quality.ScoreThreshold)
                {
                    unit.Other.Add(lockedAttribute);
                }
            }

            var xliffV12 = Xliff1Serializer.Serialize(transformation);
            fileBytes = Encoding.UTF8.GetBytes(xliffV12);
        }
        else
        {
            var content = await inputFileStream.ReadString();

            if (!Xliff2Serializer.IsXliff2(content))
                fileBytes = Encoding.UTF8.GetBytes(content);
            else
            {
                var transformation = Xliff2Serializer.Deserialize(content);
                var xliffV12 = Xliff1Serializer.Serialize(transformation);
                fileBytes = Encoding.UTF8.GetBytes(xliffV12);
            }
        }

        request.AddFile("translationFile.file", fileBytes, input.Name ?? input.File.Name);
        request.AlwaysMultipartFormData = true;

        try
        {
            var fileUploadResponse = await Client.ExecuteXtm<FileUploadResponse>(request);
            var uploadStatusResponse = await PollFileStatusAsync(project.ProjectId, fileUploadResponse.File.FileId, input.FileType);
            return new()
            {
                FileId = fileUploadResponse.File.FileId,
                JobId = fileUploadResponse.File.JobId,
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

    private async Task<byte[]> DownloadSourceFilesZip(string projectId, IEnumerable<string>? jobIds)
    {
        var url = $"{ApiEndpoints.Projects}/{projectId}/files/sources/download";

        if (jobIds?.Count() > 0)
            url += $"?{string.Join("&", jobIds.Select(x => $"jobIds={x}"))}";

        var response = await Client.ExecuteXtmWithJson(url, Method.Get, null, Creds);

        return response.RawBytes ?? [];
    }
}