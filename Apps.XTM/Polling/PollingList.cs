using System.Globalization;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Extensions;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Polling.Models.Memory;
using Apps.XTM.Polling.Models.Response;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Microsoft.AspNetCore.WebUtilities;
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
        try
        {
            var result = await ProcessProjectsPolling(request,
                $"finishedDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

            if (result.Result == null || result.Result.Projects == null)
            {
                result.FlyBird = false;
                return result;
            }

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

            if (!string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains) && result.Result?.Projects != null)
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

            if (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains))
            {
                var filteredProjects = result.Result?.Projects?.Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.Contains(projectOptionalRequest.ProjectNameContains, StringComparison.OrdinalIgnoreCase)).ToList();
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
        catch (Exception ex)
        {
            InvocationContext.Logger?.LogError($"[XTM OnProjectsFinished] Event failed. {ex.Message} - {ex.StackTrace}", null);
            throw;
        }
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

    [PollingEvent("On workflow transition (polling)", "Triggered when new jobs appear in the specified workflow steps")]
    public async Task<PollingEventResponse<WorkflowTransitionMemory, WorkflowTransitionPollingResponse>> OnWorkflowTransition(
        PollingEventRequest<WorkflowTransitionMemory> request,
        [PollingEventParameter] WorkflowTransitionPollingRequest input)
    {
        if (input.CustomerIds?.Any() != true && input.ProjectIds?.Any() != true)
            throw new PluginMisconfigurationException("Please provide either Customer IDs or Project IDs.");

        //
        // 1. Resolve active project IDs
        //
        var activeProjectIds = new List<string>();

        if (input.ProjectIds?.Any() == true)
        {
            // Verify provided projects are active
            var projects = await Client.ExecuteXtmWithJson<List<SimpleProject>>(
                $"{ApiEndpoints.Projects}?ids={string.Join(",", input.ProjectIds)}",
                Method.Get, null, Creds);

            activeProjectIds.AddRange(projects
                .Where(p => string.Equals(p.Activity, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Id));
        }
        else if (input.CustomerIds?.Any() == true)
        {
            var page = 1;
            List<SimpleProject> pageResult;
            do
            {
                pageResult = await Client.ExecuteXtmWithJson<List<SimpleProject>>(
                    $"{ApiEndpoints.Projects}?activity=ACTIVE&customerIds={string.Join(",", input.CustomerIds)}&page={page}",
                    Method.Get, null, Creds);
                activeProjectIds.AddRange(pageResult.Select(p => p.Id));
                page++;
            } while (pageResult.Count != 0);
        }

        //
        // 2. For each project, get status at STEPS level and collect jobs in target steps
        //
        var currentObserved = new HashSet<string>();
        var newJobsByProject = new Dictionary<string, List<string>>();
        var token = await Client.GetToken(Creds);

        foreach (var projectId in activeProjectIds)
        {
            var projectStatusEndpoint = $"{ApiEndpoints.Projects}/{projectId}/status";
            var queryParams = new Dictionary<string, string?>
            {
                { "fetchLevel", "STEPS" },
                { "stepReferenceNames", string.Join(",", input.WorkflowSteps) }
            };

            var statusRequest = new XTMRequest(new()
            {
                Url = Creds.Get(CredsNames.Url) + QueryHelpers.AddQueryString(projectStatusEndpoint, queryParams),
                Method = Method.Get,
            }, token);

            var statusResponse = await Client.ExecuteXtm<ProjectDetailedStatusResponse>(statusRequest);

            var jobsInSteps = statusResponse.Jobs
                .Where(j => j.Steps.Count > 0 && j.Steps.Any(s => s.Status == "IN_PROGRESS"));

            foreach (var job in jobsInSteps)
            {
                var key = $"{projectId}:{job.JobId}";
                currentObserved.Add(key);

                if (request.Memory?.ObservedJobs.Contains(key) == false)
                {
                    if (!newJobsByProject.ContainsKey(projectId))
                        newJobsByProject[projectId] = [];

                    newJobsByProject[projectId].Add(job.JobId);
                }
            }
        }

        // 3. First run — build baseline, return preflight
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new() { ObservedJobs = currentObserved }
            };
        }

        // 4. Subsequent runs — detect new jobs
        return new()
        {
            FlyBird = newJobsByProject.Count > 0,
            Memory = new() { ObservedJobs = currentObserved },
            Result = new WorkflowTransitionPollingResponse
            {
                Projects = newJobsByProject.Select(i => new WorkflowTransitionJobItem
                {
                    ProjectId = i.Key,
                    JobIds = i.Value
                })
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