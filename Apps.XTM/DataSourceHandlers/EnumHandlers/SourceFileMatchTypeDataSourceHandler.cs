using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class SourceFileMatchTypeDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("MATCH_NAMES", "Match names (replace)"),
        new("NO_MATCH", "No match (upload as new, rename if exists)"),
    ];
}
