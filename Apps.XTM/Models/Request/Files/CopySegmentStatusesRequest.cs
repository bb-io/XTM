using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.Files;

public class CopySegmentStatusesRequest
{
    [Display("Translation file", Description = "Mapped translation XLIFF returned by Download translated interoperable file.")]
    public FileReference TranslationFile { get; set; } = new();

    [Display("Target file", Description = "Full interoperable XLIFF containing the segment statuses to apply.")]
    public FileReference TargetFile { get; set; } = new();
}
