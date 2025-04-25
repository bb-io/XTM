using System.Globalization;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Polling.Models.Memory;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.XTM.Polling;

[PollingEventList]
public class PollingList(InvocationContext invocationContext) : XtmInvocable(invocationContext)
{
    [PollingEvent("On projects created (polling)", "On any new projects created")]
    public Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsCreated(
        PollingEventRequest<DateMemory> request) => ProcessProjectsPolling(request,
        $"createdDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

    [PollingEvent("On projects updated (polling)", "On any projects are updated")]
    public async Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsUpdated(
        PollingEventRequest<DateMemory> request,
        [PollingEventParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        var result = await ProcessProjectsPolling(request,
            $"modifiedDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

        if (projectOptionalRequest.ProjectId != null)
        {
            var filteredProjects = result.Result?.Projects?.Where(x => x.Id == projectOptionalRequest.ProjectId).ToList();
            if (filteredProjects != null && filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            }
            else 
            {
                result.FlyBird = false;
            }
        }

        if (!String.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains) && result.Result?.Projects != null)
        {
            var filteredProjects = new List<SimpleProject>();
            foreach (var project in result.Result?.Projects!)
            {
                var projectInfo = await Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{project.Id}", Method.Get, null, Creds);
                if (projectInfo.CustomerName.Contains(projectOptionalRequest.CustomerNameContains))
                {
                    filteredProjects.Add(project);
                }
            }

            if (filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            }
            else
            {
                result.FlyBird = false;
            }
        }

        if (!String.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains))
        {
            var filteredProjects = result.Result?.Projects?.Where(x => x.Name.Contains(projectOptionalRequest.ProjectNameContains)).ToList();
            if (filteredProjects != null && filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            }
            else
            {
                result.FlyBird = false;
            }
        }

        return result;
    }

    [PollingEvent("On projects finished (polling)", "On any projects are finished")]
    public async Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsFinished(
        PollingEventRequest<DateMemory> request,
        [PollingEventParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        var result = await ProcessProjectsPolling(request,
            $"finishedDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");
        
        if (projectOptionalRequest.ProjectId != null)
        {
            var filteredProjects = result.Result?.Projects?.Where(x => x.Id == projectOptionalRequest.ProjectId).ToList();
            if (filteredProjects != null && filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            } else 
            {
                result.FlyBird = false;
            }
        }

        if (!String.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains) && result.Result?.Projects != null)
        {
            var filteredProjects = new List<SimpleProject>();
            foreach (var project in result.Result?.Projects!)
            {
                var projectInfo =  await Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{project.Id}",Method.Get,null,Creds);
                if (projectInfo.CustomerName.Contains(projectOptionalRequest.CustomerNameContains))
                {
                    filteredProjects.Add(project);
                }
            }
            
            if  (filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            }
            else
            {
                result.FlyBird = false;
            }
        }

        if (!String.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains))
        {
            var filteredProjects = result.Result?.Projects?.Where(x => x.Name.Contains(projectOptionalRequest.ProjectNameContains)).ToList();
            if (filteredProjects != null && filteredProjects.Count > 0)
            {
                result.Result = new(filteredProjects);
            }
            else
            {
                result.FlyBird = false;
            }
        }

        return result;
    }

    [PollingEvent("On project status changed (polling)", "On status of the specific project changed")]
    public async Task<PollingEventResponse<ProjectStatusMemory, SimpleProject>> OnProjectStatusChanged(
        PollingEventRequest<ProjectStatusMemory> request,
        [PollingEventParameter] ProjectRequest project,
        [PollingEventParameter] [Display("Project status")] [StaticDataSource(typeof(ProjectStatusHandler))]
        string? status)
    {
        var projectEntity = (await Client.ExecuteXtmWithJson<List<SimpleProject>>(
            $"{ApiEndpoints.Projects}?ids={project.ProjectId}",
            Method.Get,
            null,
            Creds)).FirstOrDefault() ?? throw new InvalidOperationException("Not project found with the provided ID");

        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    Status = projectEntity.Status
                }
            };
        }

        return new()
        {
            FlyBird = request.Memory.Status != projectEntity.Status &&
                      (status == null || projectEntity.Status == status),
            Result = projectEntity,
            Memory = new()
            {
                Status = projectEntity.Status
            }
        };
    }

    [PollingEvent("On analysis finished (polling)", "On analysis of a specific project finished")]
    public async Task<PollingEventResponse<AnalysisStatusMemory, ProjectAnalysis>> OnProjectAnalysisFinished(
       PollingEventRequest<AnalysisStatusMemory> request,
       [PollingEventParameter] ProjectRequest project)
    {
        var projectAnalysisEntity = (await Client.ExecuteXtmWithJson<ProjectAnalysis>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}/analysis",
            Method.Get,
            null,
            Creds)) ?? throw new PluginMisconfigurationException("No project found with the provided ID");

        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = projectAnalysisEntity.Status.ToLower() == "finished",
                Result = projectAnalysisEntity,
                Memory = new()
                {
                    Status = projectAnalysisEntity.Status,
                    ProjectID = projectAnalysisEntity.Id
                }
            };
        }

        return new()
        {
            FlyBird = request.Memory.ProjectID == projectAnalysisEntity.Id?
            request.Memory.Status.ToLower() != "finished" && projectAnalysisEntity.Status == "finished":
            projectAnalysisEntity.Status == "finished",
            Result = projectAnalysisEntity,
            Memory = new()
            {
                Status = projectAnalysisEntity.Status,
                ProjectID = projectAnalysisEntity.Id
            }
        };
    }

    private async Task<PollingEventResponse<DateMemory, ListProjectsResponse>> ProcessProjectsPolling(
        PollingEventRequest<DateMemory> request,
        string query)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };
        }

        var result = new List<SimpleProject>();
        var page = 1;

        List<SimpleProject> response;
        do
        {
            response = await Client.ExecuteXtmWithJson<List<SimpleProject>>(
                $"{ApiEndpoints.Projects}?{query}&page={page}",
                Method.Get,
                null,
                Creds);
            result.AddRange(response);

            page++;
        } while (response.Any());

        if (!result.Any())
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };
        }

        return new()
        {
            FlyBird = true,
            Result = new(result),
            Memory = new()
            {
                LastInteractionDate = DateTime.UtcNow
            }
        };
    }
}