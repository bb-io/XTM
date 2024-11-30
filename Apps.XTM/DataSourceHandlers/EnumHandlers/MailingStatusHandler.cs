using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class MailingStatusHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {
            { "ENABLED", "Enabled" },
            { "DISABLED", "Disabled" },
        };
    }
}
