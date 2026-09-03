using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Filters.Enums;

namespace Apps.XTM.Models.Request.Files;

public class UploadSelectedSourceXliffRequest : UploadSourceFileRequest
{
    [Display("Exclude segments with states", Description = "Segments with these XLIFF states are kept in the file but marked as non-translatable. Defaults to Final.")]
    [StaticDataSource(typeof(XliffV2StateDataSourceHandler))]
    public IEnumerable<string>? ExcludeSegmentStates { get; set; } =
        [SegmentStateHelper.Serialize(SegmentState.Final)];
}
