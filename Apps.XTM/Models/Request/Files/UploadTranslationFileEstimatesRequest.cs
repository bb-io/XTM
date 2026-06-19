using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.XTM.Models.Request.Files;

public class UploadTranslationFileEstimatesRequest
{
    [Display("Mark segments as not completed by state", Description = "Mark segments as not completed if they match one of the selected states.")]
    [StaticDataSource(typeof(XliffV2StateDataSourceHandler))]
    public IEnumerable<string>? MarkSegmentsAsNotCompletedByStates { get; set; }

    [Display("Lock segments as not completed by state", Description = "Locks segments that match one of the selected states")]
    [StaticDataSource(typeof(XliffV2StateDataSourceHandler))]
    public IEnumerable<string>? LockSegmentByStates { get; set; }

    [Display("Lock segments above threshold (deprecated)", Description = "Deprecated. Use 'Lock segments as not completed by state' instead. Locks segments that have quality rating above threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? LockSegmentsAboveThreshold { get; set; }

    [Display("Mark segments under threshold as not completed (deprecated)", Description = "Deprecated. Use 'Mark segments as not completed by state' instead. Mark segments as not completed if their quality rating is below threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? MarkSegmentsUnderThresholdAsNotCompleted { get; set; }

    [Display("Segment states to mark as not completed", Description = "Specifies which segment states qualifiers. Applies only if 'Lock segments above threshold' or 'Mark segments under threshold as not completed' inputs are enabled.")]
    public IEnumerable<string>? MarkSegmentStateQualifiersAsNotCompleted { get; set; }
}
