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
        var response = await actions.ListProjects(new() { Name = context.SearchString });

        return response.Projects
            .ToDictionary(x => x.Id, x => x.Name);
    }
}