using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ProvenancePlacementDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("Translation", "Translation"),
        new("Revision", "Revision"),
    ];
}
