using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Templates;
using RestSharp;
using Blackbird.Applications.Sdk.Common;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectTemplateDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] CustomerRequest customerRequest) 
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(customerRequest.CustomerId))
            throw new PluginMisconfigurationException("Please specify customer ID first");

        var templates = await Client.ExecuteXtmWithJson<List<ProjectTemplate>>($"{ApiEndpoints.Projects}/templates",
            Method.Get,
            null,
            Creds);

        return templates
            .Where(x => x.CustomerId == customerRequest.CustomerId)
            .Where(x => context.SearchString == null || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Id, x.Name))
            .ToList();
    }
}