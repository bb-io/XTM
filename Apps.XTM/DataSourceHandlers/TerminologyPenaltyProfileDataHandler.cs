using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Terminology;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers;

public class TerminologyPenaltyProfileDataHandler(InvocationContext context)
    : XtmInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var response = await Client.ExecuteXtmWithJson<List<TerminologyPenaltyProfileResponse>>(
            ApiEndpoints.TerminologyPenaltyProfiles,
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
