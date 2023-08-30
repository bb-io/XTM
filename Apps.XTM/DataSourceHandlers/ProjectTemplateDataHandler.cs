using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Projects;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

internal class ProjectTemplateDataHandler : XtmInvocable, IAsyncDataSourceHandler
{
    public ProjectTemplateDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var templates = await Client.ExecuteXtmWithJson<List<ProjectTemplate>>($"{ApiEndpoints.Projects}/templates",
            Method.Get,
            null,
            Creds.ToArray());

        return templates
            .Where(x => context.SearchString == null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToDictionary(x => x.Id, x => x.Name);
    }
}