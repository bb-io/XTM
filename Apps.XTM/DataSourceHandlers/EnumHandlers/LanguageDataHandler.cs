using Apps.XTM.Constants;
using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class LanguageDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => Languages.All;
}