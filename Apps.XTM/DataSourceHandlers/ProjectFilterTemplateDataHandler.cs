using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Response.Templates;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectFilterTemplateDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] CustomerRequest customerRequest)
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(customerRequest.CustomerId))
            throw new PluginMisconfigurationException("Please specify customer ID first");

        var templates = await Client.ExecuteXtmWithJson<List<ProjectTemplate>>(
            $"{ApiEndpoints.Projects}/filter-templates?customerIds={customerRequest.CustomerId}",
            Method.Get,
            null,
            Creds);

        return templates
            .Where(x => context.SearchString == null || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Id, x.Name))
            .ToList();
    }
}
