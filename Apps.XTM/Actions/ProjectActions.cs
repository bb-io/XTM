using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Apps.XTM.RestUtilities;
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

        #region Actions

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
    }
}