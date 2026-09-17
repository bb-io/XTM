using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectFileDataHandler : XtmInvocable, IAsyncDataSourceItemHandler
{
    private readonly string _projectId;
    
    public ProjectFileDataHandler(InvocationContext context, [ActionParameter] ProjectRequest projectInput) 
        : base(context)
    {
        if (string.IsNullOrEmpty(projectInput.ProjectId))
            throw new PluginMisconfigurationException("Please specify a project ID");

        _projectId = projectInput.ProjectId;
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var status = await Client.ExecuteXtmWithJson<ProjectJobLevelStatusDto>(
            $"{ApiEndpoints.Projects}/{_projectId}{ApiEndpoints.Status}?fetchLevel=JOBS",
            Method.Get,
            null,
            Creds);

        return status.Jobs
            .Where(job => !string.Equals(job.CompletionStatus, "DELETED", StringComparison.OrdinalIgnoreCase))
            .Where(job => context.SearchString is null || job.FileName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(job => new DataSourceItem(job.SourceFileId.ToString(), job.FileName));
    }
}