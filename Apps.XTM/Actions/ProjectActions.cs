﻿using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using RestSharp;
using Apps.XTM.Models.Response.Metrics;
using System;
using Apps.XTM.Models.Response.User;
using Apps.XTM.Utils;
using Newtonsoft.Json;
using Blackbird.Applications.Sdk.Common.Exceptions;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace Apps.XTM.Actions;

[ActionList]
public class ProjectActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : XtmInvocable(invocationContext)
{
    private const int PageSize = 100;
    
    [Action("List projects", Description = "List all projects")]
    public async Task<ListProjectsResponse> ListProjects([ActionParameter] ListProjectsRequest request)
    {
        var page = 1;
        var hasMorePages = true;
        var allProjects = new List<SimpleProject>();

        while (hasMorePages)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "pageSize", PageSize.ToString() }
            };
            
            if (request.Name != null)
            {
                queryParams.Add("name", request.Name);
            }

            if (request.Status != null)
            {
                queryParams.Add("status", request.Status);
            }

            if (request.Activity != null)
            {
                queryParams.Add("activity", request.Activity);
            }

            if (request.CustomerIds != null)
            {
                queryParams.Add("customerIds", string.Join(",", request.CustomerIds));
            }

            if (request.CreatedFrom.HasValue)
            {
                queryParams.Add("createdDateFrom", request.CreatedFrom.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }
            if (request.CreatedTo.HasValue)
            {
                queryParams.Add("createdDateTo", request.CreatedTo.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }
            if (request.FinishedFrom.HasValue)
            {
                queryParams.Add("finishedDateFrom", request.FinishedFrom.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }
            if (request.FinishedTo.HasValue)
            {
                queryParams.Add("finishedDateTo", request.FinishedTo.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }
            if (request.ModifiedFrom.HasValue)
            {
                queryParams.Add("modifiedDateFrom", request.ModifiedFrom.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }
            if (request.ModifiedTo.HasValue)
            {
                queryParams.Add("modifiedDateTo", request.ModifiedTo.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
            }

            var endpoint = $"{ApiEndpoints.Projects}?{queryParams.ToQueryString()}";
            var response = await Client.ExecuteXtmWithJson<List<SimpleProject>?>(endpoint,
                Method.Get,
                null,
                Creds);

            if (response != null && response.Any())
            {
                allProjects.AddRange(response);
                page++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        if (request.NameExactMatch.HasValue && request.NameExactMatch.Value && !String.IsNullOrEmpty(request.Name))
        {
            if (allProjects.Any(x => x.Name == request.Name))
            {
                return new ListProjectsResponse(allProjects.Where(x => x.Name == request.Name).ToList());
            }
            return new ListProjectsResponse(new List<SimpleProject>());            
        }

        return new ListProjectsResponse(allProjects);
    }

    [Action("Get project", Description = "Get project")]
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
        string GetBridgeUrl(string eventType)
            =>
                $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}{ApplicationConstants.XtmBridgePath}"
                    .SetQueryParameter("id", Creds.GetInstanceUrlHash())
                    .SetQueryParameter("eventType", eventType);

        var parameters = new Dictionary<string, string>
        {
            { "name", input.Name.Trim() },
            { "description", input.Description?.Trim() ?? string.Empty },
            { "customerId", input.CustomerId },
            { "workflowId", input.WorkflowId },
            { "sourceLanguage", input.SourceLanguage },
            { "targetLanguages", string.Join(",", input.TargetLanguages) },
            {
                "callbacks.projectCreatedCallback",
                input.ProjectCreatedCallback ?? GetBridgeUrl(EventNames.ProjectCreated)
            },
            {
                "callbacks.projectAcceptedCallback",
                input.ProjectAcceptedCallback ?? GetBridgeUrl(EventNames.ProjectAccepted)
            },
            {
                "callbacks.projectFinishedCallback",
                input.ProjectFinishedCallback ?? GetBridgeUrl(EventNames.ProjectFinished)
            },
            { "callbacks.jobFinishedCallback", input.JobFinishedCallback ?? GetBridgeUrl(EventNames.JobFinished) },
            {
                "callbacks.analysisFinishedCallback",
                input.AnalysisFinishedCallback ?? GetBridgeUrl(EventNames.AnalysisFinished)
            },
            {
                "callbacks.workflowTransitionCallback",
                input.WorkflowTransitionCallback ?? GetBridgeUrl(EventNames.WorkflowTransition)
            },
            {
                "callbacks.invoiceStatusChangedCallback",
                input.InvoiceStatusChangedCallback ?? GetBridgeUrl(EventNames.InvoiceStatusChanged)
            }
        };

        if (input.DueDate.HasValue)
        {
            parameters.Add("dueDate", input.DueDate.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
        }

        try 
        {
            return Client.ExecuteXtmWithFormData<CreateProjectResponse>(ApiEndpoints.Projects,
            Method.Post,
            parameters,
            Creds);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("Incorrect parameters"))
            {
                throw new PluginMisconfigurationException("Please check the input values on this action." + e.Message);
            }
            else
            {
                throw new PluginApplicationException(e.Message);
            }
        }
        
    }

    [Action("Create new project from template",
        Description = "Create a new project using an existing project template")]
    public Task<CreateProjectResponse> CreateProjectFromTemplate(
        [ActionParameter] CreateProjectFromTemplateRequest input)
    {
        string GetBridgeUrl(string eventType)
           =>
               $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}{ApplicationConstants.XtmBridgePath}"
                   .SetQueryParameter("id", Creds.GetInstanceUrlHash())
                   .SetQueryParameter("eventType", eventType);
        var parameters = new Dictionary<string, string>
        {
            { "name", input.Name.Trim() },
            { "description", input.Description?.Trim() ?? string.Empty  },
            { "customerId", input.CustomerId },
            { "templateId", input.TemplateId },
            {
                "callbacks.projectCreatedCallback",
                input.ProjectCreatedCallback ?? GetBridgeUrl(EventNames.ProjectCreated)
            },
            {
                "callbacks.projectAcceptedCallback",
                input.ProjectAcceptedCallback ?? GetBridgeUrl(EventNames.ProjectAccepted)
            },
            {
                "callbacks.projectFinishedCallback",
                input.ProjectFinishedCallback ?? GetBridgeUrl(EventNames.ProjectFinished)
            },
            { "callbacks.jobFinishedCallback", input.JobFinishedCallback ?? GetBridgeUrl(EventNames.JobFinished) },
            {
                "callbacks.analysisFinishedCallback",
                input.AnalysisFinishedCallback ?? GetBridgeUrl(EventNames.AnalysisFinished)
            },
            {
                "callbacks.workflowTransitionCallback",
                input.WorkflowTransitionCallback ?? GetBridgeUrl(EventNames.WorkflowTransition)
            },
            {
                "callbacks.invoiceStatusChangedCallback",
                input.InvoiceStatusChangedCallback ?? GetBridgeUrl(EventNames.InvoiceStatusChanged)
            }
        };

        if (input.DueDate.HasValue)
        {
            parameters.Add("dueDate", input.DueDate.Value.ToString("yyyy-MM-ddThh:mm:ssZ"));
        }
        try 
        {
            return Client.ExecuteXtmWithFormData<CreateProjectResponse>(ApiEndpoints.Projects,
                Method.Post,
                parameters,
                Creds);
        } 
        catch (Exception e) 
        {
            if (e.Message.Contains("Incorrect parameters"))
            {
                throw new PluginMisconfigurationException("Please check the input values on this action." + e.Message);
            }
            else 
            {
                throw new PluginApplicationException(e.Message);
            }
        }
        
    }

    [Action("Clone project", Description = "Create a new project based on the provided project")]
    public Task<CreateProjectResponse> CloneProject([ActionParameter] CloneProjectRequest input)
    {
        var parameters = new Dictionary<string, string>
        {
            { "originId", input.OriginId }
        };

        if (input.Name != null)
            parameters.Add("name", input.Name.Trim());

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

    [Action("Get project estimates", Description = "Get specific project estimates")]
    public Task<ProjectEstimates> GetProjectEstimates([ActionParameter] ProjectRequest project)
    {
        return Client.ExecuteXtmWithJson<ProjectEstimates>($"{ApiEndpoints.Projects}/{project.ProjectId}/proposal",
            Method.Get,
            null,
            Creds);
    }

    [Action("Download metrics", Description = "Download metrics file for a specific file in XLSX format")]
    public async Task<FileResponse> DownloadMetrics(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadMetricsRequest input)
    {
        var endpoint =
            $"{ApiEndpoints.Projects}/{project.ProjectId}/metrics/files/download?metricsFilesType={input.MetricsFilesType}";
        var response = await Client.ExecuteXtmWithJson(endpoint, Method.Get, null, Creds);

        using var stream = new MemoryStream(response.RawBytes);
        var file = await fileManagementClient.UploadAsync(stream,
            response.ContentType ?? MediaTypeNames.Application.Octet, $"{project.ProjectId}.xlsx");
        return new(file);
    }

    [Action("Get bundle metrics", Description = "Get metrics for a specific bundle")]
    public async Task<List<MetricsResponse>> GetBundleMetrics([ActionParameter] ProjectRequest project, [ActionParameter] BundleMetricsRequest request)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Metrics}{ApiEndpoints.Bundles}";

        if(request.JobId is not null)
        {
            endpoint += $"?jobIds={request.JobId}";
        }

        return await Client.ExecuteXtmWithJson<List<MetricsResponse>>(endpoint, Method.Get, null, Creds);
    }

    [Action("Get project metrics", Description = "Get metrics for a specific project")]
    public async Task<MetricByLanguagesResponse> GetProjectMetrics([ActionParameter] ProjectRequest project, [ActionParameter] TargetLanguagesMetricsRequest languages )
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/{ApiEndpoints.Metrics}";

        if (languages.TargetLanguages is not null && languages.TargetLanguages.Any())
        {
            endpoint += "?targetLanguages=" + string.Join(",", languages.TargetLanguages);
        }

        try 
        {
            var response = await Client.ExecuteXtmWithJson<List<MetricsByLanguage>>(endpoint, Method.Get, null, Creds);

            return new MetricByLanguagesResponse
            {
                Metrics = response,
                TotalWords = response?.Count > 0 ? response.Sum(x => x.coreMetrics.totalWords): 0           
            };

        } catch (Exception ex) 
        {
            if (ex.Message.Contains("Please wait for analysis"))
            {
                throw new PluginMisconfigurationException("This request cannot be processed at the moment because the specified project is under analysis. " +
                    "Consider using a Checkpoint or adding retries in the error handling tab");
            }
            else
            {
                throw new PluginApplicationException(ex.Message);
            }
        }
        
    }

    [Action("Get project completion", Description = "Get project completion for a specific project")]
    public async Task<ProjectCompletionResponse> GetProjectCompletion([ActionParameter] ProjectRequest project)
    {
        var userId = Creds.Get(CredsNames.UserId);

        var loginApi = new loginAPI
        {
            client = Creds.Get(CredsNames.Client),
            userIdSpecified = true,
            userId = ParseId(userId),
            password = Creds.Get(CredsNames.Password),
        };

        var xtmProjectDescriptorApi = new xtmProjectDescriptorAPI
        {
            id = ParseId(project.ProjectId),
            idSpecified = true,
            projectExternalIdSpecified = false,
            externalIdSpecified = false
        };

        checkProjectCompletionResponse result = await this.ProjectManagerMTOClient.checkProjectCompletionAsync(loginApi, xtmProjectDescriptorApi, new xtmCheckProjectCompletionOptionsAPI());
         return new(result);
    }
    
    [Action("Get project status", Description = "Get project status for a specific project")]
    public async Task<ProjectStatusResponse> GetProjectStatus([ActionParameter] ProjectRequest project)
    {
        return await ErrorHandler.ExecuteWithErrorHandlingAsync(async () =>
        {
            var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Status}";
            return await Client.ExecuteXtmWithJson<ProjectStatusResponse>(endpoint, Method.Get, null, Creds);
        });
       
    }
    
    [Action("Get project users", Description = "Get users assigned to a specific project")]
    public async Task<ProjectUsersResponse> GetProjectUsers([ActionParameter] ProjectRequest project)
    {
        var endpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Users}";
        var projectUsers = await Client.ExecuteXtmWithJson<ProjectUsers>(endpoint, Method.Get, null, Creds);
        
        var projectUsersResponse = new ProjectUsersResponse
        {
            ProjectManager = await GetUserById(projectUsers.ProjectManager.UserId),
            ProjectCreator = await GetUserById(projectUsers.ProjectCreator.UserId)
        };

        foreach (var linguist in projectUsers.Linguists)
        {
            projectUsersResponse.Linguists.Add(await GetUserById(linguist.UserId));
        }

        return projectUsersResponse;
    }
    
    [Action("Get project details", Description = "Get project details for a specific project")]
    public async Task<ProjectDetailsResponse> GetProjectDetails([ActionParameter] ProjectRequest project)
    {
        var projectCompletion = await GetProjectCompletion(project);
        var projectStatus = await GetProjectStatus(project);
        var getProjectEstimates = await GetProjectEstimates(project);
        
        return new ProjectDetailsResponse
        {
            ProjectCompletion = projectCompletion,
            ProjectStatus = projectStatus,
            ProjectEstimates = getProjectEstimates
        };
    }
}