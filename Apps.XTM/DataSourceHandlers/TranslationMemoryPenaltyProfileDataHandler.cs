using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers;

public class TranslationMemoryPenaltyProfileDataHandler(InvocationContext invocationContext)
    : XtmInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var response = await Client.ExecuteXtmWithJson<List<TranslationMemoryPenaltyProfileResponse>>(
            ApiEndpoints.TMPenaltyProfiles, 
            RestSharp.Method.Get,
            null,
            Creds
        );

        return response
            .Select(x => new DataSourceItem(x.Id, x.Name))
            .Where(x => 
                context.SearchString == null || 
                x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
