using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class SegmentLockingTypeDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "DISABLED", "Disabled" },
        { "ANY_100_MATCH", "Any 100% match" },
        { "APPROVED_100_MATCH", "Approved 100% match" },
        { "XLIFFDOC_TRANSLATED", "XLIFF document translated" },
        { "XLIFFDOC_PROOFED", "XLIFF document proofed" },
        { "XLIFFDOC_VALIDATED", "XLIFF document validated" }
    };
}