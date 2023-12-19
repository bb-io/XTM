using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class MetricsFilesTypeDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "INITIAL", "Initial" },
        { "CURRENT", "Current" }
    };
}