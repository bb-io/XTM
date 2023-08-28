using System.IO.Compression;
using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Apps.XTM.RestUtilities;
using RestSharp;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Actions
{
    [ActionList]
    public class ProjectActions
    {
        #region Fields

        private static XTMClient _client;

        #endregion

        #region Constructors

        static ProjectActions()
        {
            _client = new();
        }

        #endregion

        #region Project actions

        [Action("List projects", Description = "List all projects")]
        public async Task<ListProjectsResponse> ListProjects(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
        {
            var response = await _client.ExecuteXtm<List<SimpleProject>>(ApiEndpoints.Projects,
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());

            return new(response);
        }
        
        [Action("Get project", Description = "Get project by id")]
        public Task<FullProject> GetProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project)
        {
            return _client.ExecuteXtm<FullProject>($"{ApiEndpoints.Projects}/{project.ProjectId}",
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Create new project", Description = "Create new project")]
        public Task<CreateProjectResponse> CreateProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] CreateProjectRequest input)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "name", input.Name },
                { "description", input.Description },
                { "customerId", input.CustomerId },
                { "workflowId", input.WorkflowId },
                { "sourceLanguage", input.SourceLanguge },
                { "targetLanguages", input.TargetLanguage },
            };

            return _client.ExecuteXtm<CreateProjectResponse>(ApiEndpoints.Projects,
                Method.Post,
                parameters,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Create new project from template", Description = "Create a new project using an existing project template")]
        public Task<CreateProjectResponse> CreateProjectFromTemplate(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] CreateProjectFromTemplateRequest input)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "name", input.Name },
                { "description", input.Description },
                { "customerId", input.CustomerId.ToString() },
                { "templateId", input.TemplateId }
            };

            return _client.ExecuteXtm<CreateProjectResponse>(ApiEndpoints.Projects,
                Method.Post,
                parameters,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Clone project", Description = "Create a new project based on the provided project")]
        public Task<CreateProjectResponse> CloneProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] CloneProjectRequest input)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "name", input.Name },
                { "originId", input.OriginId },
            };

            return _client.ExecuteXtm<CreateProjectResponse>($"{ApiEndpoints.Projects}/clone",
                Method.Post,
                parameters,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Update project", Description = "Update specific project")]
        public Task<ManageEntityResponse> UpdateProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] UpdateProjectRequest input)
        {
            return _client.ExecuteXtm<ManageEntityResponse>($"{ApiEndpoints.Projects}/{project.ProjectId}",
                Method.Put,
                bodyObj: input,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Delete project", Description = "Delete specific project")]
        public Task DeleteProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project)
        {
            return _client.ExecuteXtm($"{ApiEndpoints.Projects}/{project.ProjectId}",
                Method.Delete,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Get project estimates", Description = "Get specific project estimates")]
        public Task<ProjectEstimates> GetProjectEstimates(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project)
        {
            return _client.ExecuteXtm<ProjectEstimates>($"{ApiEndpoints.Projects}/{project.ProjectId}/proposal",
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());
        }

        #endregion

        #region Project files actions

        [Action("Download source files as ZIP",
            Description = "Download the source files for project or specific jobs as ZIP")]
        public async Task<FileResponse> DownloadSourceFilesAsZip(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] [Display("Job ids")] int[]? jobIds)
        {
            var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/download";

            if (jobIds != null)
                url += $"?{string.Join("&", jobIds.Select(x => $"jobIds={x}"))}";

            var response = await _client.ExecuteXtm(url,
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());

            return new(new(response.RawBytes)
            {
                Name = $"Project-{project.ProjectId}SourceFiles.zip",
                ContentType = response.ContentType ?? MediaTypeNames.Application.Octet
            });
        }

        [Action("Download source files", Description = "Download the source files for project or specific jobs")]
        public async Task<SourceFilesResponse> DownloadSourceFiles(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] [Display("Job ids")] int[]? jobIds)
        {
            var zipFile = await DownloadSourceFilesAsZip(authenticationCredentialsProviders, project, jobIds);

            var files = new List<File>();

            using var zipStream = new MemoryStream(zipFile.File.Bytes);
            using var zipArchive = new ZipArchive(zipStream);
            foreach (var entry in zipArchive.Entries)
            {
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var fileData = new File(fileBytes)
                {
                    Name = entry.Name,
                    ContentType = MediaTypeNames.Application.Octet
                };

                files.Add(fileData);
            }

            return new(files);
        }

        [Action("Download project file", Description = "Download a single, generated project file based on its ID")]
        public async Task<FileResponse> DownloadProjectFile(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] [Display("File id")] int fileId)
        {
            var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/{fileId}/download?fileScope=JOB";

            var response = await _client.ExecuteXtm(url,
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());

            return new(new(response.RawBytes)
            {
                Name = $"Project-{project.ProjectId} File-{fileId}",
                ContentType = response.ContentType ?? MediaTypeNames.Application.Octet
            });
        }

        [Action("Upload source file", Description = "Upload source files for a project")]
        public async Task<CreateProjectResponse> UploadSourceFile(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] UploadSourceFileRequest input)
        {
            var translationType = input.TranslationType;
            if (!ParametersValues.TranslationType.Contains(translationType))
                throw new(
                    $"Wrong translation type value, it should be one of {string.Join(',', ParametersValues.TranslationType)}");
            
            var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/sources/upload";
            var creds = authenticationCredentialsProviders.ToArray();

            var token = await _client.GetToken(creds);

            var request = new XTMRequest(new()
            {
                Url = creds.Get("url") + url,
                Method = Method.Post
            }, token);

            var parameters = new Dictionary<string, string>()
            {
                { "files[0].name", input.Name ?? input.File.Name },
                { "files[0].workflowId", input.WorkflowId.ToString() },
                { "files[0].translationType", translationType },
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
                    parameters.Add($"files[0].tagIds[{i}]", tags[i].ToString());
            }

            if (input.TargetLanguages is not null)
            {
                var langs = input.TargetLanguages.ToArray();
                for (var i = 0; i < langs.Length; i++)
                    parameters.Add($"files[0].targetLanguages[{i}]", langs[i]);
            }

            parameters.ToList().ForEach(x
                => request.AddParameter(x.Key, x.Value));
            request.AddFile("files[0].file", input.File.Bytes, input.Name ?? input.File.Name);
            request.AlwaysMultipartFormData = true;

            return await _client.ExecuteXtm<CreateProjectResponse>(request);
        }

        [Action("Upload translation file", Description = "Upload translation file to project")]
        public async Task<CreateProjectResponse> UploadTranslationFile(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ProjectRequest project,
            [ActionParameter] UploadTranslationFileInput input)
        {
            var segmentStatus = input.SegmentStatusApproving.ToUpper();

            if (!ParametersValues.SegmentStatusApproving.Contains(segmentStatus))
                throw new(
                    $"Wrong segment status approving value, it should be one of {string.Join(',', ParametersValues.SegmentStatusApproving)}");

            var url = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/translations/upload";
            var creds = authenticationCredentialsProviders.ToArray();

            var token = await _client.GetToken(creds);

            var parameters = new Dictionary<string, string>()
            {
                { "fileType", input.FileType },
                { "jobId", input.JobId.ToString() },
                { "translationFile.name", input.Name ?? input.File.Name },
                { "xliffOptions.autopopulation", input.Autopopulation ? "ENABLED" : "DISABLED" },
                { "xliffOptions.segmentStatusApproving", segmentStatus },
            };

            if (!input.Autopopulation)
                parameters.Add("workflowStepName", input.WorkflowStepName);

            var request = new XTMRequest(new()
            {
                Url = creds.Get("url") + url,
                Method = Method.Post
            }, token);

            parameters.ToList().ForEach(x
                => request.AddParameter(x.Key, x.Value, encode: false));
            request.AddFile("translationFile.file", input.File.Bytes, input.Name ?? input.File.Name);
            request.AlwaysMultipartFormData = true;

            return await _client.ExecuteXtm<CreateProjectResponse>(request);
        }

        #endregion
    }
}