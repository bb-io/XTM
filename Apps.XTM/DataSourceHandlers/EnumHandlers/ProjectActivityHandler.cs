using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ProjectActivityHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        {"ACTIVE", "Active"},
        {"ARCHIVED", "Archived"},
        {"AUTO_ARCHIVED", "Auto archived"},
        {"DELETED", "Deleted"},
        {"INACTIVE", "Inactive"},
        {"ACTIVATING", "Activating"},
        {"ARCHIVING", "Archiving"},
        {"AUTO_ARCHIVING", "Auto archiving"}
    };
}