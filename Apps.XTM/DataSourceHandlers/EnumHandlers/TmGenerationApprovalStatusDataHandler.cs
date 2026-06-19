using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class TmGenerationApprovalStatusDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "ALL", "All" },
        { "APPROVED", "Approved" },
        { "NOT_APPROVED", "Not approved" }
    };
}
