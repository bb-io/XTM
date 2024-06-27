using System.Globalization;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Polling.Models.Memory;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.XTM.Polling;

[PollingEventList]
public class PollingList : XtmInvocable
{
    public PollingList(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [PollingEvent("On projects created (polling)", "On any new projects created")]
    public Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsCreated(
        PollingEventRequest<DateMemory> request) => ProcessProjectsPolling(request,
        $"createdDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

    [PollingEvent("On projects updated (polling)", "On any projects are updated")]
    public Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsUpdated(
        PollingEventRequest<DateMemory> request) => ProcessProjectsPolling(request,
        $"modifiedDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

    [PollingEvent("On projects finished (polling)", "On any projects are finished")]
    public Task<PollingEventResponse<DateMemory, ListProjectsResponse>> OnProjectsFinished(
        PollingEventRequest<DateMemory> request) => ProcessProjectsPolling(request,
        $"finishedDateFrom={request.Memory?.LastInteractionDate.ToString("o", CultureInfo.InvariantCulture)}");

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