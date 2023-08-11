using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ProposalApprovalStatusDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "CONFIRMED", "Confirmed" },
        { "NOT_CONFIRMED", "Not confirmed" },
        { "NOT_REQUIRED", "Not required" }
    };
}