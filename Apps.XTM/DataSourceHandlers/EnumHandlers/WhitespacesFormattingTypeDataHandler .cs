using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class WhitespacesFormattingTypeDataHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {
            { "REMOVE_LEADING_AND_TRAILING_WHITESPACE", "Remove leading and trailing whitespace" },
            { "REMOVE_LEADING_AND_INNER_WHITESPACE",    "Remove leading and inner whitespace" },
            { "REMOVE_REDUNDANT_INNER_WHITESPACE",      "Remove redundant inner whitespace" },
            { "KEEP_ALL_WHITESPACES",                   "Keep all whitespaces" }
        };
    }
}
