using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class SegmentStatusApprovingDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "NONE", "None" },
        { "ACCORDINGLY_TO_STATE", "Accordingly to state" },
        { "ALL_UPDATED_SEGMENTS", "All updated segments" },
        { "ALL_SEGMENTS", "All segments" }
    };
}