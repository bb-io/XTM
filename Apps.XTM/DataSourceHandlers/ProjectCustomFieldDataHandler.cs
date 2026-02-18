using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectCustomFieldDataHandler : XtmInvocable, IAsyncDataSourceHandler
{
    public ProjectCustomFieldDataHandler(InvocationContext invocationContext)
        : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var endpoint = $"custom-fields/project";

        var response = await Client.ExecuteXtmWithJson<List<ProjectCustomFieldDefinitionResponse>>(
            endpoint,
            Method.Get,
            null,
            Creds);

        return response
            .Where(x => context.SearchString == null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Id.ToString(), x => x.Name);
    }
}
