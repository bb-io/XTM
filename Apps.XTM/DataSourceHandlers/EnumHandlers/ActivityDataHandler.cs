using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ActivityDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        {"DISABLED", "Disabled"},
        {"ACTIVE", "Active"},
        {"PM", "PM"},
        {"DELETED", "Deleted"},
    };
}