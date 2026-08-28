using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ProvenanceTypeDataSourceHandler : IStaticDataSourceItemHandler
{
    public const string Translation = "translation";
    public const string Review = "review";

    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(Translation, "Translation"),
        new(Review, "Review"),
    ];
}
