using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Utils;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectDataHandler(InvocationContext invocationContext)
    : XtmInvocable(invocationContext), IAsyncDataSourceHandler
{
    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "page", 1.ToString() },
            { "pageSize", 20.ToString() },
            { "sort", "createdDate,desc" }
        };

        if (!string.IsNullOrWhiteSpace(context.SearchString))
            queryParams.Add("name", context.SearchString);

        var endpoint = $"{ApiEndpoints.Projects}?{queryParams.ToQueryString()}";

        var response = await Client.ExecuteXtmWithJson<List<SimpleProject>?>(endpoint, Method.Get, null, Creds);

        return response?.ToDictionary(x => x.Id, x => x.Name) ?? [];
    }
}