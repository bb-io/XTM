using Apps.XTM.Actions;
using Apps.XTM.Invocables;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectDataHandler(InvocationContext invocationContext)
    : XtmInvocable(invocationContext), IAsyncDataSourceHandler
{
    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var actions = new ProjectActions(InvocationContext, null!);
        var response = await actions.ListProjects();

        return response.Projects
            .Where(x => context.SearchString == null || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToDictionary(x => x.Id, x => x.Name);
    }
}