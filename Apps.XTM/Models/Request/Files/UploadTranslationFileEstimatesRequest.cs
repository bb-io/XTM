using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Files;

public class UploadTranslationFileEstimatesRequest
{
    [Display("Lock segments above threshold", Description = "Locks segments that have quality rating above threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? LockSegmentsAboveThreshold { get; set; }

    [Display("Mark segments under threshlold as not-completed", Description = "Mark segments as not completed if their quality rating is below threshold. Works with any XLIFF's standard quality attributes.")]
    public bool? MarkSegmentsUnderThresholdAsNotCompleted { get; set; }
}
