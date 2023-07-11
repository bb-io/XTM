using System.IO.Compression;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Apps.XTM.RestUtilities;
using Newtonsoft.Json;
using RestSharp;

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

        [Action("Get project", Description = "Get project by id")]
        public Task<FullProject> GetProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            long projectId)
        {
            return _client.ExecuteXtm<FullProject>($"{ApiEndpoints.Projects}/{projectId}",
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
                { "customerId", input.CustomerId.ToString() },
                { "workflowId", input.WorkflowId.ToString() },
                { "sourceLanguage", input.SourceLanguge },
            };

            input.TargetLanguges.ToList().ForEach(x => parameters.Add("targetLanguages", x));

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
                { "originId", input.OriginId.ToString() },
            };

            return _client.ExecuteXtm<CreateProjectResponse>($"{ApiEndpoints.Projects}/clone",
                Method.Post,
                parameters,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Update project", Description = "Update specific project")]
        public Task<ManageEntityResponse> UpdateProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            long projectId,
            [ActionParameter] UpdateProjectRequest input)
        {
            return _client.ExecuteXtm<ManageEntityResponse>($"{ApiEndpoints.Projects}/{projectId}",
                Method.Put,
                bodyObj: input,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Delete project", Description = "Delete specific project")]
        public Task DeleteProject(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            long projectId)
        {
            return _client.ExecuteXtm($"{ApiEndpoints.Projects}/{projectId}",
                Method.Delete,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());
        }

        [Action("Get project estimates", Description = "Get specific project estimates")]
        public Task<ProjectEstimates> GetProjectEstimates(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            long projectId)
        {
            return _client.ExecuteXtm<ProjectEstimates>($"{ApiEndpoints.Projects}/{projectId}/proposal",
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
            [ActionParameter] [Display("Project id")]
            int projectId,
            [ActionParameter] [Display("Job ids")] int[]? jobIds)
        {
            var url = $"{ApiEndpoints.Projects}/{projectId}/files/sources/download";

            if (jobIds != null)
                url += $"?{string.Join("&", jobIds.Select(x => $"jobIds={x}"))}";

            var response = await _client.ExecuteXtm(url,
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());

            return new(response.RawBytes);
        }

        [Action("Download source files", Description = "Download the source files for project or specific jobs")]
        public async Task<SourceFilesResponse> DownloadSourceFiles(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            int projectId,
            [ActionParameter] [Display("Job ids")] int[]? jobIds)
        {
            var zipFile = await DownloadSourceFilesAsZip(authenticationCredentialsProviders, projectId, jobIds);

            var files = new List<FileData>();

            using var zipStream = new MemoryStream(zipFile.File);
            using var zipArchive = new ZipArchive(zipStream);
            foreach (var entry in zipArchive.Entries)
            {
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var fileData = new FileData(entry.Name, fileBytes);

                files.Add(fileData);
            }

            return new(files);
        }

        [Action("Download project file", Description = "Download a single, generated project file based on its ID")]
        public async Task<FileResponse> DownloadProjectFile(
            IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] [Display("Project id")]
            int projectId,
            [ActionParameter] [Display("File id")] int fileId)
        {
            var url = $"{ApiEndpoints.Projects}/{projectId}/files/{fileId}/download?fileScope=JOB";

            var response = await _client.ExecuteXtm(url,
                Method.Get,
                bodyObj: null,
                authenticationCredentialsProviders.ToArray());

            return new(response.RawBytes);
        }
        //
        // [Action("Upload source file", Description = "Upload source files for a project")]
        // public async Task<CreateProjectResponse> UploadSourceFile(
        //     IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        //     [ActionParameter] [Display("Project id")]
        //     int projectId,
        //     [ActionParameter] UploadSourceFileRequest input)
        // {
        //     var url = $"{ApiEndpoints.Projects}/{projectId}/files/sources/upload";
        //     var creds = authenticationCredentialsProviders.ToArray();
        //
        //     var token = await _client.GetToken(creds);
        //
        //     var request = new XTMRequest(new()
        //     {
        //         Url = creds.Get("url") + url,
        //         Method = Method.Post
        //     }, token);
        //
        //     //request.AddObject(input, "File", "Name");
        //     request.AddParameter("files", JsonConvert.SerializeObject(input), ParameterType.RequestBody);
        //     request.AlwaysMultipartFormData = true;
        //
        //     return await _client.ExecuteXtm<CreateProjectResponse>(request);
        // }
        //
        // [Action("Upload translation file", Description = "Upload translation file to project")]
        // public async Task<CreateProjectResponse> UploadTranslationFile(
        //     IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        //     [ActionParameter] [Display("Project id")]
        //     int projectId,
        //     [ActionParameter] UploadTranslationFileInput input)
        // {
        //     var url = $"{ApiEndpoints.Projects}/{projectId}/files/translations/upload";
        //     var creds = authenticationCredentialsProviders.ToArray();
        //
        //     var token = await _client.GetToken(creds);
        //
        //     var params2 = new Dictionary<string, string>()
        //     {
        //         { "fileType", input.FileType },
        //         { "jobId", input.JobId.ToString() },
        //         { "workflowStepName", input.WorkflowStepName },
        //         // {"translationFile", JsonConvert.SerializeObject(new TranslationFile()
        //         // {
        //         //     Name = input.Name,
        //         //     File = input.File
        //         // })},
        //         // {"xliffOptions", JsonConvert.SerializeObject(new XliffOptions()
        //         // {
        //         //     Autopopulation = input.Autopopulation ? "ENABLED" : "DISABLED",
        //         //     SegmentStatusApproving = input.SegmentStatusApproving,
        //         // }) },
        //     };
        //     // var obj = new UploadTranslationFileRequest()
        //     // {
        //     //     FileType = input.FileType,
        //     //     JobId = input.JobId,
        //     //     WorkflowStepName = input.WorkflowStepName,
        //     //     TranslationFile = new()
        //     //     {
        //     //         Name = input.Name,
        //     //        // File = input.File
        //     //     },
        //     //     XliffOptions = new()
        //     //     {
        //     //         Autopopulation = input.Autopopulation ? "ENABLED" : "DISABLED",
        //     //         SegmentStatusApproving = input.SegmentStatusApproving
        //     //     }
        //     // };
        //     var request = new XTMRequest(new()
        //     {
        //         Url = creds.Get("url") + url,
        //         Method = Method.Post
        //     }, token);
        //
        //     // request.AddParameter(JsonConvert.SerializeObject(obj), ParameterType.RequestBody);
        //     params2.ToList().ForEach(x => request.AddParameter(x.Key, x.Value, encode: false));
        //     request.AddParameter(new BodyParameter("translationFile", JsonConvert.SerializeObject(new TranslationFile()
        //     {
        //         Name = input.Name,
        //         File = input.File
        //     }), ContentType.Json));
        //
        //     request.AddParameter(new BodyParameter("xliffOptions", JsonConvert.SerializeObject(new XliffOptions()
        //     {
        //         Autopopulation = input.Autopopulation ? "ENABLED" : "DISABLED",
        //         SegmentStatusApproving = input.SegmentStatusApproving,
        //     }), ContentType.Json));
        //
        //     request.AlwaysMultipartFormData = true;
        //
        //     return await _client.ExecuteXtm<CreateProjectResponse>(request);
        // }

        #endregion
    }
}