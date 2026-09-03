using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.RestUtilities;
using Apps.XTM.Utils;
using Apps.XTM.Webhooks.Models.Response;
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
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.WebUtilities;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Apps.XTM.Actions;

[ActionList]
public class FileActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : XtmInvocable(invocationContext)
{
    private readonly IFileManagementClient _fileManagementClient = fileManagementClient;

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

    [Action("Add provenance metadata", Description = "Add translation or review provenance to units in a translated file using generated job data and workflow assignments. By default, person attribution applies only to signed-off segments. Output is XLIFF 2.2")]
    public async Task<FileResponse> AddMetadata(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] AddMetadataRequest input)
    {
        var attributionMode = string.IsNullOrWhiteSpace(input.AttributeSegmentsToUser)
            ? SegmentAttributionDataSourceHandler.OnlyConfirmed
            : input.AttributeSegmentsToUser;
        if (attributionMode is not (
            SegmentAttributionDataSourceHandler.All
            or SegmentAttributionDataSourceHandler.OnlyConfirmed
            or SegmentAttributionDataSourceHandler.OnlyChanged
            or SegmentAttributionDataSourceHandler.None))
        {
            throw new PluginMisconfigurationException(
                "Attribute segments to user must be all segments, only confirmed segments, only changed segments, or no segments.");
        }

        var provenanceTypeOverride = string.IsNullOrWhiteSpace(input.ProvenanceType)
            ? null
            : input.ProvenanceType.Trim().ToLowerInvariant();
        if (provenanceTypeOverride is not null
            and not ProvenanceTypeDataSourceHandler.Translation
            and not ProvenanceTypeDataSourceHandler.Review)
        {
            throw new PluginMisconfigurationException(
                "Provenance type must be translation or review.");
        }

        await using var inputStream = await _fileManagementClient.DownloadAsync(input.File);
        var inputBytes = await inputStream.GetByteData();
        using var inputVersionStream = new MemoryStream(inputBytes);
        var isXliff2 = Xliff2Serializer.IsXliff2(inputVersionStream, out _);
        inputVersionStream.Position = 0;
        var isXliff1 = Xliff1Serializer.IsXliff1(inputVersionStream, out _);
        if (!isXliff1 && !isXliff2)
            throw new PluginMisconfigurationException("Translated file must be a valid XLIFF 1 or XLIFF 2 file.");

        var loadResult = Transformation.Load(new MemoryStream(inputBytes), input.File.Name, input.File.ContentType);
        if (!loadResult.Success)
            throw new PluginMisconfigurationException($"Translated XLIFF could not be parsed. Details: {loadResult.Error}");

        var transformation = loadResult.Value!;
        var inputDocument = XDocument.Load(new MemoryStream(inputBytes), LoadOptions.PreserveWhitespace);
        var inputUnitSources = new List<(string? Id, List<XElement> Sources)>();
        if (isXliff1)
        {
            foreach (var transUnit in inputDocument.Descendants().Where(x => x.Name.LocalName == "trans-unit"))
            {
                var segmentedSource = transUnit.Elements().FirstOrDefault(x => x.Name.LocalName == "seg-source");
                inputUnitSources.Add(((string?)transUnit.Attribute("id"), segmentedSource is null
                    ? transUnit.Elements().Where(x => x.Name.LocalName == "source").Take(1).ToList()
                    : segmentedSource.Descendants()
                        .Where(x => x.Name.LocalName == "mrk" && (string?)x.Attribute("mtype") == "seg")
                        .ToList()));
            }
        }
        else
        {
            foreach (var unit in inputDocument.Descendants().Where(x => x.Name.LocalName == "unit"))
            {
                inputUnitSources.Add(((string?)unit.Attribute("id"), unit.Descendants()
                    .Where(x => x.Name.LocalName == "source"
                        && x.Parent?.Name.LocalName is "segment" or "ignorable")
                    .ToList()));
            }
        }

        var inputUnits = transformation.GetUnits().ToList();
        if (inputUnits.Count != inputUnitSources.Count || inputUnitSources.Any(x => x.Sources.Count == 0))
            throw new PluginMisconfigurationException("Translated XLIFF unit structure could not be mapped to its source segments.");

        var inputUnitsById = isXliff2
            ? inputUnits
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id!)
                .Where(x => x.Count() == 1)
                .ToDictionary(x => x.Key, x => x.Single())
            : [];
        if (isXliff2 && (inputUnitsById.Count != inputUnits.Count
            || inputUnitSources.Any(x => string.IsNullOrWhiteSpace(x.Id) || !inputUnitsById.ContainsKey(x.Id))))
        {
            throw new PluginMisconfigurationException(
                "Translated XLIFF 2 units must have unique IDs so provenance can be mapped safely.");
        }

        var downloadUrl = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/download".WithQuery(
            new Dictionary<string, string>
            {
                { "fileScope", "JOB" },
                { "fileType", "XLIFF" },
                { "jobIds", input.JobId },
            });

        RestResponse downloadResponse;
        try
        {
            downloadResponse = await Client.ExecuteXtmWithJson(downloadUrl, Method.Get, null, Creds);
        }
        catch (PluginApplicationException ex) when (ex.Message.Contains("Unavailable data", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("file was not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "Generated XLIFF file is unavailable. Generate XLIFF files for this job in XTM before running Add metadata.");
        }

        using var downloadedZip = new MemoryStream(downloadResponse.RawBytes ?? []);
        IEnumerable<BlackbirdZipEntry> downloadedFiles;
        try
        {
            downloadedFiles = await downloadedZip.GetFilesFromZip();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"Generated XLIFF ZIP could not be read. Details: {ex.Message}");
        }

        var downloadedXliff = downloadedFiles.FirstOrDefault(x =>
            Path.GetExtension(x.UploadName).Equals(".xlf", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(x.UploadName).Equals(".xliff", StringComparison.OrdinalIgnoreCase));
        if (downloadedXliff is null)
            throw new PluginMisconfigurationException("Generated files do not contain an XLIFF file for the selected job.");

        var xtmBytes = await downloadedXliff.FileStream.GetByteData();
        var xtmDocument = XDocument.Load(new MemoryStream(xtmBytes), LoadOptions.PreserveWhitespace);
        var xtmSegments = new List<(
            XElement Source,
            string? State,
            string? Qualifier,
            bool Changed,
            bool ChangedFromEmpty)>();

        foreach (var transUnit in xtmDocument.Descendants().Where(x => x.Name.LocalName == "trans-unit"))
        {
            var segmentedSource = transUnit.Elements().FirstOrDefault(x => x.Name.LocalName == "seg-source");
            var sourceSegments = segmentedSource is null
                ? transUnit.Elements().Where(x => x.Name.LocalName == "source").Take(1).ToList()
                : segmentedSource.Descendants()
                    .Where(x => x.Name.LocalName == "mrk" && (string?)x.Attribute("mtype") == "seg")
                    .ToList();
            var targetContainer = transUnit.Elements().FirstOrDefault(x => x.Name.LocalName == "target");

            foreach (var source in sourceSegments)
            {
                var segmentId = (string?)source.Attribute("mid");
                var target = segmentId is null
                    ? targetContainer
                    : targetContainer?.Descendants().FirstOrDefault(x =>
                        x.Name.LocalName == "mrk" && (string?)x.Attribute("mid") == segmentId);
                var state = (string?)target?.Attribute("state") ?? (string?)targetContainer?.Attribute("state");
                var qualifier = (string?)target?.Attribute("state-qualifier")
                    ?? (string?)targetContainer?.Attribute("state-qualifier");
                var alternatives = transUnit.Elements()
                    .Where(x => x.Name.LocalName == "alt-trans")
                    .Where(x => segmentId is null
                        || sourceSegments.Count == 1
                        || string.Equals((string?)x.Attribute("mid"), segmentId, StringComparison.Ordinal)
                        || x.Elements().Where(y => y.Name.LocalName == "target").Descendants().Any(y =>
                            y.Name.LocalName == "mrk"
                            && string.Equals((string?)y.Attribute("mid"), segmentId, StringComparison.Ordinal)))
                    .ToList();
                var matchingAlternative = qualifier?.ToLowerInvariant() switch
                {
                    "mt-suggestion" => alternatives.FirstOrDefault(x =>
                        string.Equals((string?)x.Attribute("extype"), "MACHINE-TRANSLATION", StringComparison.OrdinalIgnoreCase)),
                    "exact-match" => alternatives.FirstOrDefault(x =>
                        string.Equals((string?)x.Attribute("extype"), "exact-match", StringComparison.OrdinalIgnoreCase)),
                    _ => alternatives.FirstOrDefault(),
                };
                var alternativeTargetContainer = matchingAlternative?.Elements()
                    .FirstOrDefault(x => x.Name.LocalName == "target");
                var alternativeTarget = segmentId is null
                    ? alternativeTargetContainer
                    : alternativeTargetContainer?.Descendants().FirstOrDefault(x =>
                        x.Name.LocalName == "mrk" && (string?)x.Attribute("mid") == segmentId);
                if (alternativeTarget is null && alternativeTargetContainer is not null
                    && (sourceSegments.Count == 1
                        || string.Equals(
                            (string?)matchingAlternative?.Attribute("mid"),
                            segmentId,
                            StringComparison.Ordinal)))
                {
                    alternativeTarget = alternativeTargetContainer;
                }

                var targetContent = target is null
                    ? string.Empty
                    : string.Concat(target.Nodes().Select(x => x.ToString(SaveOptions.DisableFormatting)));
                var alternativeContent = alternativeTarget is null
                    ? string.Empty
                    : string.Concat(alternativeTarget.Nodes().Select(x => x.ToString(SaveOptions.DisableFormatting)));
                var targetPreservesWhitespace = target?.AncestorsAndSelf()
                    .FirstOrDefault(x => x.Attribute(XNamespace.Xml + "space") is not null)?
                    .Attribute(XNamespace.Xml + "space")?.Value == "preserve";
                var alternativePreservesWhitespace = alternativeTarget?.AncestorsAndSelf()
                    .FirstOrDefault(x => x.Attribute(XNamespace.Xml + "space") is not null)?
                    .Attribute(XNamespace.Xml + "space")?.Value == "preserve";
                if (!targetPreservesWhitespace)
                    targetContent = targetContent.Trim();
                if (!alternativePreservesWhitespace)
                    alternativeContent = alternativeContent.Trim();
                var changed = target is not null && (alternativeTarget is null
                    ? targetContent.Length > 0
                    : !string.Equals(targetContent, alternativeContent, StringComparison.Ordinal));
                var changedFromEmpty = changed
                    && targetContent.Length > 0
                    && alternativeContent.Length == 0;

                xtmSegments.Add((source, state, qualifier, changed, changedFromEmpty));
            }
        }

        if (inputUnitSources.Sum(x => x.Sources.Count) != xtmSegments.Count)
            throw new PluginMisconfigurationException(
                $"Translated XLIFF contains {inputUnitSources.Sum(x => x.Sources.Count)} segments, but generated XTM XLIFF contains {xtmSegments.Count}. Provenance cannot be mapped safely.");

        var assignedBundles = new List<WorkflowAssignmentBundleResponse>();
        WorkflowStepResponse? selectedStepDefinition = null;
        if (attributionMode != SegmentAttributionDataSourceHandler.None || provenanceTypeOverride is null)
        {
            var projectWorkflows = await Client.ExecuteXtmWithJson<List<ProjectWorkflowResponse>>(
                $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow?jobIds={input.JobId}",
                Method.Get,
                null,
                Creds);
            var projectSteps = projectWorkflows.SelectMany(x => x.Steps).ToList();
            var stepIds = projectSteps
                .Select(x => x.Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            var stepDefinitions = stepIds.Count == 0
                ? []
                : await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(
                    $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}?ids={string.Join("&ids=", stepIds)}",
                    Method.Get,
                    null,
                    Creds);
            var automaticStepIds = stepDefinitions
                .Where(x => x.Type?.Contains("AUTOMATIC", StringComparison.OrdinalIgnoreCase) == true)
                .Select(x => x.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var manualSteps = projectSteps.Where(x => !automaticStepIds.Contains(x.Id)).ToList();
            var selectedStep = string.IsNullOrWhiteSpace(input.WorkflowStep)
                ? manualSteps.LastOrDefault()
                : manualSteps.FirstOrDefault(x =>
                    string.Equals(x.ReferenceStepName, input.WorkflowStep, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Name, input.WorkflowStep, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.DisplayStepName, input.WorkflowStep, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(input.WorkflowStep) && selectedStep is null)
                throw new PluginMisconfigurationException(
                    $"Workflow step '{input.WorkflowStep}' does not exist in this job or is automatic.");

            selectedStepDefinition = selectedStep is null
                ? null
                : stepDefinitions.FirstOrDefault(x =>
                    string.Equals(x.Id, selectedStep.Id, StringComparison.OrdinalIgnoreCase));

            if (attributionMode != SegmentAttributionDataSourceHandler.None && selectedStep is not null)
            {
                var assignments = await Client.ExecuteXtmWithJson<WorkflowAssignmentsResponse>(
                    $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow/assignment?jobIds={input.JobId}",
                    Method.Get,
                    null,
                    Creds);
                assignedBundles = assignments.Jobs
                    .FirstOrDefault(x => x.JobId == input.JobId)?
                    .Steps.FirstOrDefault(x =>
                        WorkflowStepNamesMatch(x.ReferenceStepName, selectedStep.ReferenceStepName)
                        || WorkflowStepNamesMatch(x.Name, selectedStep.Name)
                        || WorkflowStepNamesMatch(x.DisplayStepName, selectedStep.DisplayStepName))?
                    .Bundles ?? [];
            }
        }

        var xtmSegmentIndex = 0;
        for (var unitIndex = 0; unitIndex < inputUnits.Count; unitIndex++)
        {
            var inputUnit = isXliff2
                ? inputUnitsById[inputUnitSources[unitIndex].Id!]
                : inputUnits[unitIndex];
            string? person = null;
            string? specificTool = null;
            var changedExistingTarget = false;

            foreach (var inputSource in inputUnitSources[unitIndex].Sources)
            {
                var xtmSegment = xtmSegments[xtmSegmentIndex];
                if (!string.Equals(
                    NormalizeElementText(inputSource),
                    NormalizeElementText(xtmSegment.Source),
                    StringComparison.Ordinal))
                {
                    throw new PluginMisconfigurationException(
                        $"Segment {xtmSegmentIndex + 1} source differs between translated XLIFF and generated XTM XLIFF. Provenance cannot be mapped safely.");
                }

                var appliesToPerson = attributionMode switch
                {
                    SegmentAttributionDataSourceHandler.All => true,
                    SegmentAttributionDataSourceHandler.OnlyConfirmed =>
                        string.Equals(xtmSegment.State, "signed-off", StringComparison.OrdinalIgnoreCase),
                    SegmentAttributionDataSourceHandler.OnlyChanged => xtmSegment.Changed,
                    _ => false,
                };
                var segmentPosition = xtmSegmentIndex + 1;
                var assignedBundle = assignedBundles.FirstOrDefault(x =>
                    (!x.From.HasValue || segmentPosition >= x.From.Value)
                    && (!x.To.HasValue || segmentPosition <= x.To.Value)
                    && !string.IsNullOrWhiteSpace(x.UserId)
                    && !string.IsNullOrWhiteSpace(x.UserName));
                if (person is null && appliesToPerson && assignedBundle is not null)
                    person = $"{assignedBundle.UserName} (ID {assignedBundle.UserId})";

                if (specificTool is null && !string.IsNullOrWhiteSpace(xtmSegment.Qualifier))
                {
                    var qualifier = Regex.Replace(
                        xtmSegment.Qualifier.Trim().ToLowerInvariant(),
                        "[-_]+",
                        " ");
                    specificTool = $"XTM ({qualifier})";
                }

                if (xtmSegment.Changed && !xtmSegment.ChangedFromEmpty)
                    changedExistingTarget = true;

                xtmSegmentIndex++;
            }

            var provenanceType = provenanceTypeOverride
                ?? selectedStepDefinition?.Role?.ToUpperInvariant() switch
                {
                    "TRANSLATE" => ProvenanceTypeDataSourceHandler.Translation,
                    "REVIEW" or "CORRECT" or "LQA" => ProvenanceTypeDataSourceHandler.Review,
                    _ => changedExistingTarget
                        ? ProvenanceTypeDataSourceHandler.Review
                        : ProvenanceTypeDataSourceHandler.Translation,
                };
            var provenance = provenanceType == ProvenanceTypeDataSourceHandler.Review
                ? inputUnit.Provenance.Review
                : inputUnit.Provenance.Translation;
            provenance.Person = person;
            provenance.PersonReference = null;
            provenance.Tool = person is not null
                ? "XTM"
                : specificTool ?? "XTM";
            provenance.ToolReference = null;
        }

        var outputName = input.File.Name;
        var output = await _fileManagementClient.UploadAsync(
            transformation.Serialize().ToStream(),
            "application/xliff+xml",
            outputName);

        return new FileResponse(output);
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
        var fileName = input.Name?.Trim() ?? input.File.Name ??
            throw new PluginMisconfigurationException("File name is required");

        await using var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        using var seekableStream = new MemoryStream(fileBytes);

        if (Xliff2Serializer.IsXliff2(seekableStream, out var xliffNode))
        {
            var transformation = Xliff2Serializer.Deserialize(xliffNode);
            var xliffV12 = Xliff1Serializer.Serialize(transformation);
            fileBytes = Encoding.UTF8.GetBytes(xliffV12);
        }

        return await UploadSourceFileBytes(project, input, fileBytes, fileName);
    }

    [Action("Upload source XLIFF excluding selected states", Description = "Upload a complete XLIFF file while excluding segments in selected states from translation")]
    public async Task<UploadSelectedSourceXliffResponse> UploadSelectedSourceXliff(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UploadSelectedSourceXliffRequest input)
    {
        var fileName = input.Name?.Trim() ?? input.File.Name ??
            throw new PluginMisconfigurationException("File name is required");

        await using var fileStream = await _fileManagementClient.DownloadAsync(input.File);
        var sourceBytes = await fileStream.GetByteData();
        var prepared = XliffSourceSelection.Prepare(sourceBytes, input.ExcludeSegmentStates);

        await using var preparedStream = new MemoryStream(prepared.Content);
        var preparedFile = await _fileManagementClient.UploadAsync(
            preparedStream,
            "application/xliff+xml",
            fileName);

        CreateProjectResponse? uploadResponse = null;
        if (prepared.SegmentsLeft > 0)
            uploadResponse = await UploadSourceFileBytes(project, input, prepared.Content, fileName);

        return new UploadSelectedSourceXliffResponse
        {
            Name = uploadResponse?.Name ?? fileName,
            ProjectId = uploadResponse?.ProjectId ?? project.ProjectId,
            Jobs = uploadResponse?.Jobs ?? [],
            File = preparedFile,
            Uploaded = uploadResponse != null,
            SegmentsExcluded = prepared.SegmentsExcluded,
            SegmentsTotal = prepared.SegmentsTotal,
            SegmentsLeft = prepared.SegmentsLeft,
            ApproximateWordCount = prepared.ApproximateWordCount,
        };
    }

    private async Task<CreateProjectResponse> UploadSourceFileBytes(
        ProjectRequest project,
        UploadSourceFileRequest input,
        byte[] fileBytes,
        string fileName)
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
            { "files[0].name", fileName },
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

    private static string NormalizeElementText(XElement element)
    {
        return Regex.Replace(element.Value, @"\s+", " ").Trim();
    }

    private static bool WorkflowStepNamesMatch(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left)
               && !string.IsNullOrWhiteSpace(right)
               && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
