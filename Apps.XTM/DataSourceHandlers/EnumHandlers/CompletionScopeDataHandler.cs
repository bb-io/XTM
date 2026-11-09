using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class CompletionScopeDataHandler : IStaticDataSourceItemHandler
{
    public const string Project = "PROJECT";
    public const string TargetLanguage = "TARGET_LANGUAGE";

    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(Project, "Entire project"),
        new(TargetLanguage, "Each target language")
    ];
}
