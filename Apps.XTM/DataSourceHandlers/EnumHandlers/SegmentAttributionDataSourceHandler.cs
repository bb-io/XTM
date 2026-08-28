using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class SegmentAttributionDataSourceHandler : IStaticDataSourceItemHandler
{
    public const string All = "all";
    public const string OnlyConfirmed = "only_confirmed";
    public const string OnlyChanged = "only_changed";
    public const string None = "none";

    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(All, "All segments"),
        new(OnlyConfirmed, "Only confirmed segments"),
        new(OnlyChanged, "Only changed segments"),
        new(None, "No segments"),
    ];
}
