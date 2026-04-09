using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Files;

public class UploadTranslationFileEstimatesRequest
{
    [Display("Lock segments above threshold", Description = "Locks segments that have quality rating above threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? LockSegmentsAboveThreshold { get; set; }

    [Display("Mark segments under threshold as not completed", Description = "Mark segments as not completed if their quality rating is below threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? MarkSegmentsUnderThresholdAsNotCompleted { get; set; }

    [Display("Segment states to mark as not completed", Description = "Specifies which segment states qualifiers. Applies only if 'Lock segments above threshold' or 'Mark segments under threshold as not completed' inputs are enabled.")]
    public IEnumerable<string>? MarkSegmentStateQualifiersAsNotCompleted { get; set; }
}
