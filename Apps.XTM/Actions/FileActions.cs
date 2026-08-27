using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Models.Response.Provenance;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.Models;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Shared;
using DocumentFormat.OpenXml.Drawing.Diagrams;
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

    [Action("Add latest provenance data", Description = "Enrich an XLIFF file with latest file-level translation and revision provenance from XTM")]
    public async Task<FileResponse> AddLatestProvenanceData(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] AddLatestProvenanceDataRequest input)
    {
        var extension = Path.GetExtension(input.File.Name);
        if (!extension.Equals(".xlf", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xliff", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "Only XLIFF files with an .xlf or .xliff extension are supported.");
        }

        if (input.Placement is not null
            && !input.Placement.Equals("Translation", StringComparison.OrdinalIgnoreCase)
            && !input.Placement.Equals("Revision", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "Provenance placement must be Translation or Revision.");
        }

        await using var inputStream = await _fileManagementClient.DownloadAsync(input.File);
        var inputBytes = await inputStream.GetByteData();
        Transformation transformation;

        try
        {
            using var detectionStream = new MemoryStream(inputBytes);
            if (Xliff2Serializer.IsXliff2(detectionStream, out var xliff2Node))
            {
                transformation = Xliff2Serializer.Deserialize(xliff2Node);
            }
            else
            {
                using var xliff1DetectionStream = new MemoryStream(inputBytes);
                if (!Xliff1Serializer.IsXliff1(xliff1DetectionStream, out var xliff1Node))
                    throw new PluginMisconfigurationException(
                        "File is not valid XLIFF 1.x or XLIFF 2.x content.");

                transformation = Xliff1Serializer.Deserialize(xliff1Node);
            }
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PluginMisconfigurationException(
                $"File could not be parsed as XLIFF: {ex.Message}");
        }

        var fileTransformations = transformation.Children.OfType<Transformation>().ToList();
        if (fileTransformations.Count == 0)
            fileTransformations.Add(transformation);

        var targetLanguages = fileTransformations
            .Select(file => file.TargetLanguage)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language!.Replace('-', '_'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var status = await Client.ExecuteXtmWithJson<ProjectDetailedStatusResponse>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Status}?fetchLevel=STEPS",
            Method.Get,
            null,
            Creds);

        var projectJobs = status.Jobs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(input.JobId))
        {
            var suppliedJob = status.Jobs.FirstOrDefault(job =>
                job.JobId.Equals(input.JobId, StringComparison.OrdinalIgnoreCase));
            if (suppliedJob is null)
            {
                throw new PluginMisconfigurationException(
                    $"Job {input.JobId} does not belong to project {project.ProjectId}.");
            }

            projectJobs = [suppliedJob];
        }
        else if (targetLanguages.Count > 0)
        {
            projectJobs = projectJobs.Where(job => targetLanguages.Any(language =>
            {
                var jobLanguage = job.TargetLanguage.Replace('-', '_');
                return jobLanguage.Equals(language, StringComparison.OrdinalIgnoreCase)
                    || jobLanguage.StartsWith(language + "_", StringComparison.OrdinalIgnoreCase)
                    || language.StartsWith(jobLanguage + "_", StringComparison.OrdinalIgnoreCase);
            }));
        }

        var selectedJobs = projectJobs.ToList();
        if (selectedJobs.Count == 0)
        {
            throw new PluginMisconfigurationException(
                "No project jobs match supplied job ID or XLIFF target language. Specify a matching Job ID.");
        }

        var selectedJobIds = selectedJobs.Select(job => long.Parse(job.JobId)).ToHashSet();
        var statistics = await Client.ExecuteXtmWithJson<List<ProjectStatisticsResponse>>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}/statistics",
            Method.Get,
            null,
            Creds);
        var projectWorkflows = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow",
            Method.Get,
            null,
            Creds);
        var workflowDefinitions = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(
            $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}?activity=ALL",
            Method.Get,
            null,
            Creds);
        var metrics = await Client.ExecuteXtmWithJson<List<JobMetricsResponse>>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}/metrics/jobs?jobIds={string.Join(',', selectedJobIds)}",
            Method.Get,
            null,
            Creds);

        var assignments = new List<WorkflowAssignmentJobResponse>();
        var assignmentPage = 1;
        while (true)
        {
            var assignmentResponse = await Client.ExecuteXtmWithJson(
                $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow/assignment?jobIds={string.Join(',', selectedJobIds)}&page={assignmentPage}&pageSize=100",
                Method.Get,
                null,
                Creds);
            var page = JsonConvert.DeserializeObject<WorkflowAssignmentsResponse>(assignmentResponse.Content ?? string.Empty)
                ?? new WorkflowAssignmentsResponse();
            assignments.AddRange(page.Jobs);

            var totalItemsHeader = assignmentResponse.Headers?.FirstOrDefault(header =>
                string.Equals(header.Name, "xtm-total-items-count", StringComparison.OrdinalIgnoreCase));
            var totalItems = totalItemsHeader is not null
                && int.TryParse(totalItemsHeader.Value?.ToString(), out var parsedTotalItems)
                    ? parsedTotalItems
                    : assignments.Count;
            if (page.Jobs.Count == 0 || assignments.Count >= totalItems)
                break;

            assignmentPage++;
        }

        var definitionRoles = workflowDefinitions
            .Where(step => !string.IsNullOrWhiteSpace(step.Role))
            .GroupBy(step => step.Id)
            .ToDictionary(group => group.Key, group => group.First().Role!, StringComparer.OrdinalIgnoreCase);
        var stepRoles = projectWorkflows
            .SelectMany(workflow => workflow.Steps)
            .Where(step => definitionRoles.ContainsKey(step.Id))
            .GroupBy(step => step.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => definitionRoles[group.First().Id],
                StringComparer.OrdinalIgnoreCase);

        if (stepRoles.Count == 0)
        {
            throw new PluginApplicationException(
                "XTM workflow steps could not be mapped to translation or revision roles.");
        }

        var humanEvidence = new List<(string Role, string Person, long CompletedAt, long JobId, string StepName)>();
        foreach (var languageStatistics in statistics)
        {
            var normalizedLanguage = languageStatistics.TargetLanguage.Replace('-', '_');
            if (string.IsNullOrWhiteSpace(input.JobId)
                && targetLanguages.Count > 0 && !targetLanguages.Any(language =>
                normalizedLanguage.Equals(language, StringComparison.OrdinalIgnoreCase)
                || normalizedLanguage.StartsWith(language + "_", StringComparison.OrdinalIgnoreCase)
                || language.StartsWith(normalizedLanguage + "_", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var userStatistics in languageStatistics.UsersStatistics)
            foreach (var stepStatistics in userStatistics.StepsStatistics)
            {
                var stepName = new[]
                {
                    stepStatistics.WorkflowStepName,
                    stepStatistics.StepReferenceName,
                    stepStatistics.ReferenceStepName
                }.FirstOrDefault(name => stepRoles.ContainsKey(name));
                if (stepName is null)
                    continue;

                var role = stepRoles[stepName];
                if (role is not ("TRANSLATE" or "REVIEW" or "CORRECT"))
                    continue;

                foreach (var jobStatistics in stepStatistics.JobsStatistics.Where(job =>
                    selectedJobIds.Contains(job.JobId) && job.LastCompletionDate > 0))
                {
                    var assignmentStep = assignments
                        .Where(job => job.JobId == jobStatistics.JobId)
                        .SelectMany(job => job.Steps)
                        .FirstOrDefault(step => new[] { step.Name, step.DisplayStepName, step.StepReferenceName }
                            .Any(name => name.Equals(stepName, StringComparison.OrdinalIgnoreCase)));
                    if (assignmentStep is null)
                        continue;

                    var directAssignments = assignmentStep.Bundles
                        .Where(bundle => bundle.UserId.HasValue && !string.IsNullOrWhiteSpace(bundle.UserName))
                        .OrderBy(bundle => bundle.From)
                        .ThenBy(bundle => bundle.UserId)
                        .ToList();
                    var matchedAssignment = directAssignments.FirstOrDefault(bundle =>
                        bundle.UserId == userStatistics.UserId
                        || bundle.UserName!.Equals(userStatistics.Username, StringComparison.OrdinalIgnoreCase)
                        || bundle.UserName.Equals(userStatistics.UserDisplayName, StringComparison.OrdinalIgnoreCase));
                    var person = matchedAssignment?.UserName
                        ?? directAssignments.FirstOrDefault()?.UserName
                        ?? userStatistics.UserDisplayName
                        ?? userStatistics.Username;
                    if (string.IsNullOrWhiteSpace(person))
                        continue;

                    humanEvidence.Add((role, person, jobStatistics.LastCompletionDate, jobStatistics.JobId, stepName));
                }
            }
        }

        var latestTranslation = humanEvidence
            .Where(record => record.Role == "TRANSLATE")
            .OrderByDescending(record => record.CompletedAt)
            .FirstOrDefault();
        var latestRevision = humanEvidence
            .Where(record => record.Role is "REVIEW" or "CORRECT")
            .OrderByDescending(record => record.CompletedAt)
            .FirstOrDefault();
        var hasHumanTranslation = !string.IsNullOrWhiteSpace(latestTranslation.Person);
        var hasRevision = !string.IsNullOrWhiteSpace(latestRevision.Person);
        var hasMachineTranslation = metrics.Any(metric =>
            selectedJobIds.Contains(metric.JobId) && metric.CoreMetrics.MachineTranslationSegments > 0);

        ProvenanceRecord? translationRecord = hasHumanTranslation
            ? new ProvenanceRecord { Person = latestTranslation.Person, Tool = "XTM" }
            : hasMachineTranslation
                ? new ProvenanceRecord { Tool = "XTM machine translation" }
                : null;
        ProvenanceRecord? revisionRecord = hasRevision
            ? new ProvenanceRecord { Person = latestRevision.Person, Tool = "XTM" }
            : null;

        if (translationRecord is null && revisionRecord is null)
        {
            throw new PluginApplicationException(
                "No usable completed human workflow or machine-translation evidence was found for selected XTM jobs.");
        }

        if (input.Placement is null)
        {
            foreach (var file in fileTransformations)
            {
                if (translationRecord is not null)
                    file.Provenance.Translation = translationRecord;
                if (revisionRecord is not null)
                    file.Provenance.Review = revisionRecord;
            }
        }
        else
        {
            var newestRecord = hasRevision && latestRevision.CompletedAt > latestTranslation.CompletedAt
                ? revisionRecord!
                : translationRecord ?? revisionRecord!;
            foreach (var file in fileTransformations)
            {
                if (input.Placement.Equals("Translation", StringComparison.OrdinalIgnoreCase))
                    file.Provenance.Translation = newestRecord;
                else
                    file.Provenance.Review = newestRecord;
            }
        }

        string serialized;
        try
        {
            serialized = Xliff2Serializer.Serialize(transformation);
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"Enriched XLIFF could not be serialized: {ex.Message}");
        }

        using var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
        var mediaType = string.IsNullOrWhiteSpace(input.File.ContentType)
            ? MimeTypes.GetMimeType(input.File.Name)
            : input.File.ContentType;
        var outputFile = await _fileManagementClient.UploadAsync(outputStream, mediaType, input.File.Name);
        return new FileResponse(outputFile);
    }

    [Action("Generate files", Description = "Generate files for a project")]
    public async Task<ListGeneratedFilesResponse> GenerateFiles(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] GenerateFileRequest input)
    {
        if (input.JobIds?.Any() == true && input.ActiveWorkflowSteps?.Any() == true)
            throw new PluginMisconfigurationException("Please specify either Job IDs or active workflow steps, not both, as action can filter either by Job IDs or active workflow step.");

        if (input.JobIds?.Any() != true && input.FileType != "MULTI_EXCEL")
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
            { "fileType", input.FileType }
        };

        if (input.FileType != "MULTI_EXCEL")
        { queryParameters.Add("jobIds", string.Join(",", input.JobIds)); }

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
        Description = "Download source files for a project or selected jobs")]
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

    [Action("Download source files", Description = "Download source files for a project or selected jobs")]
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

    [Action("Download project file", Description = "Download a generated project file")]
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

    [Action("Download all project files", Description = "Download project files")]
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

        if (input.JobIds?.Any() == true)
            queryParameters.Add("jobIds", string.Join(",", input.JobIds));

        if (input.TargetLanguages?.Any() == true)
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

    [Action("Download translated files", Description = "Download translated files from a project")]
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

    [Action("Download reference files", Description = "Download reference files from a project")]
    public async Task<DownloadFilesResponse<XtmSourceFileDescription>> DownloadReferenceFiles(
    [ActionParameter] ProjectRequest projectInput)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{projectInput.ProjectId}/files/reference-materials/download";

        var response = await Client.ExecuteXtmWithJson(endpoint, Method.Get, null, Creds);
        if (response.RawBytes == null || response.RawBytes.Length == 0)
            throw new PluginMisconfigurationException("The file is empty");

        await using var fileStream = new MemoryStream(response.RawBytes);

        IEnumerable<BlackbirdZipEntry> files;
        try
        {
            files = await fileStream.GetFilesFromZip();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException(
                $"The file returned from server is empty or damaged. Please check and try again. Message: {ex.Message}");
        }

        var filesWithData = new List<FileWithData<XtmSourceFileDescription>>();

        foreach (var file in files)
        {
            var uploadedFile = await _fileManagementClient.UploadAsync(
                file.FileStream,
                MimeTypes.GetMimeType(file.UploadName),
                file.UploadName
            );

            filesWithData.Add(new FileWithData<XtmSourceFileDescription>
            {
                Content = uploadedFile,
                FileDescription = new XtmSourceFileDescription
                {
                    FileName = file.UploadName,
                    FileId = projectInput.ProjectId,
                    JobIds = []
                }
            });
        }

        return new DownloadFilesResponse<XtmSourceFileDescription>(filesWithData);
    }

    [Action("Upload source file", Description = "Upload a source file to a project")]
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
        };

        if (!string.IsNullOrEmpty(input.WorkflowId))
            parameters.Add("files[0].workflowId", input.WorkflowId);

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
       

        await using var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        using var seekableStream = new MemoryStream(fileBytes);

        if (Xliff2Serializer.IsXliff2(seekableStream, out var xliffNode))
        {
            var transformation = Xliff2Serializer.Deserialize(xliffNode);
            var xliffV12 = Xliff1Serializer.Serialize(transformation);
            fileBytes = Encoding.UTF8.GetBytes(xliffV12);
        }

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
    Description = "Upload a reference file to a project")]
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

    [Action("Upload translation file", Description = "Upload a translation file to a project")]
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
            // 2026-06-19 This branch is deprecated in favour of updating by segment state
            var loadResult = Transformation.Load(inputFileStream, input.File.Name);
            if (!loadResult.Success)
            {
                throw new PluginApplicationException(loadResult.Error);
            }

            var transformation = loadResult.Value;
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

            foreach (var unit in transformation.GetUnits())
            {
                if (estimatesRequest.MarkSegmentStateQualifiersAsNotCompleted is not null)
                {
                    foreach (var segment in unit.Segments)
                    {
                        var stateQualifier = segment.TargetAttributes.FirstOrDefault(a => a.Name == "state-qualifier");
                        var sholdMarkAsNonCompleted = estimatesRequest
                            .MarkSegmentStateQualifiersAsNotCompleted
                            .Contains(stateQualifier?.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                        if (sholdMarkAsNonCompleted)
                            segment.State = null;
                        continue;
                    }
                }
            }

            var xliffV12 = Xliff1Serializer.Serialize(transformation);
            fileBytes = Encoding.UTF8.GetBytes(xliffV12);
        } else if (estimatesRequest.LockSegmentByStates?.Any() == true
            || estimatesRequest.MarkSegmentsAsNotCompletedByStates?.Any() == true)
        {
            var lockSegmentsByStates = estimatesRequest.LockSegmentByStates?
                .Select(SegmentStateHelper.ToSegmentState)
                .Where(state => state != null)
                .Select(state => state!.Value)
                .ToHashSet() ?? [];

            var markSegmentsAsNotCompletedByStates = estimatesRequest.MarkSegmentsAsNotCompletedByStates?
                    .Select(SegmentStateHelper.ToSegmentState)
                    .Where(state => state != null)
                    .Select(state => state!.Value)
                    .ToHashSet() ?? [];

            var loadResult = Transformation.Load(inputFileStream, input.File.Name);
            if (!loadResult.Success)
            {
                throw new PluginApplicationException(loadResult.Error);
            }

            var xtmNamespace = XNamespace.Get("urn:xliff-xtm-extensions");
            var lockedAttribute = new XAttribute(xtmNamespace + "locked", "yes");

            var transformation = loadResult.Value;

            foreach (var unit in transformation.GetUnits())
            {
                foreach (var segment in unit.Segments)
                {
                    if (lockSegmentsByStates.Contains(segment.State ?? SegmentState.Initial))
                    {
                        unit.Other.Add(lockedAttribute);
                    }
                    if (markSegmentsAsNotCompletedByStates.Contains(segment.State ?? SegmentState.Initial))
                    {
                        segment.State = null;
                    }
                }
            }

            foreach (var unit in transformation.GetUnits())
            {
                if (estimatesRequest.MarkSegmentStateQualifiersAsNotCompleted is not null)
                {
                    foreach (var segment in unit.Segments)
                    {
                        var stateQualifier = segment.TargetAttributes.FirstOrDefault(a => a.Name == "state-qualifier");
                        var sholdMarkAsNonCompleted = estimatesRequest
                            .MarkSegmentStateQualifiersAsNotCompleted
                            .Contains(stateQualifier?.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                        if (sholdMarkAsNonCompleted)
                            segment.State = null;
                        continue;
                    }
                }
            }

            var xliffV12 = Xliff1Serializer.Serialize(transformation);
            fileBytes = Encoding.UTF8.GetBytes(xliffV12);
        }
        else
        {
            var content = inputFileStream.ReadString();

            if (!Xliff2Serializer.IsXliff2(inputFileStream, out _))
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
