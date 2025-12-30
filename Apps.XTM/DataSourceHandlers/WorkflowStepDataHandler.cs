using RestSharp;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers;

public class WorkflowStepDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] ProjectOptionalRequest project) 
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(project.ProjectId))
        {
            var workflowEndpoint = $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow";
            var workflowResponse = await Client.ExecuteXtmWithJson<List<ProjectWorkflowResponse>>(
                workflowEndpoint, 
                Method.Get, 
                null,
                Creds
            );

            if (workflowResponse.Count != 0)
            {
                return workflowResponse.SelectMany(w => w.Steps)
                    .Where(x => 
                        context.SearchString == null ||
                        (x.Name != null && x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                    )
                    .Take(20)
                    .Select(x => new DataSourceItem(x.Name, x.DisplayStepName));
            }
        }

        var stepsEndpoint = $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}";
        var stepsResponse = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(
            stepsEndpoint,
            Method.Get,
            null,
            Creds
        );

        return stepsResponse
            .Where(x => 
                context.SearchString == null || 
                x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
            )
            .Take(20)
            .Select(x => new DataSourceItem(x.Name, x.Name));
    }
}