using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ManualWorkflowStepDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] ProjectRequest project,
    [ActionParameter] AddMetadataRequest input)
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project.ProjectId))
            throw new PluginMisconfigurationException("Please specify project first.");
        if (string.IsNullOrWhiteSpace(input.JobId))
            throw new PluginMisconfigurationException("Please specify job ID first.");

        var workflows = await Client.ExecuteXtmWithJson<List<ProjectWorkflowResponse>>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}/workflow".WithQuery(
                new Dictionary<string, string> { { "jobIds", input.JobId } }),
            Method.Get,
            null,
            Creds);

        var projectSteps = workflows.SelectMany(x => x.Steps).ToList();
        var stepIds = projectSteps
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (stepIds.Count == 0)
            return [];

        var stepDefinitions = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(
            $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}?ids={string.Join("&ids=", stepIds)}",
            Method.Get,
            null,
            Creds);
        var automaticStepIds = stepDefinitions
            .Where(x => x.Type?.Contains("AUTOMATIC", StringComparison.OrdinalIgnoreCase) == true)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return projectSteps
            .Where(x => !automaticStepIds.Contains(x.Id))
            .Where(x => context.SearchString is null
                || x.DisplayStepName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => string.IsNullOrWhiteSpace(x.ReferenceStepName) ? x.Name : x.ReferenceStepName)
            .Take(20)
            .Select(x => new DataSourceItem(
                string.IsNullOrWhiteSpace(x.ReferenceStepName) ? x.Name : x.ReferenceStepName,
                x.DisplayStepName));
    }
}
