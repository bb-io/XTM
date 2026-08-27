using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectJobDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] ProjectRequest project)
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project.ProjectId))
            throw new Exception("Please specify project first.");

        var status = await Client.ExecuteXtmWithJson<ProjectDetailedStatusResponse>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}{ApiEndpoints.Status}?fetchLevel=JOBS",
            Method.Get,
            null,
            Creds);

        return status.Jobs
            .Where(job => context.SearchString is null
                || job.JobId.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                || job.FileName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                || job.TargetLanguage.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(job => new DataSourceItem(job.JobId, $"{job.FileName} ({job.TargetLanguage})"));
    }
}
