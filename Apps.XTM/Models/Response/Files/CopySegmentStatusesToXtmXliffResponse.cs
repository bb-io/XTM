using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public class CopySegmentStatusesToXtmXliffResponse
{
    [Display("Translation file", Description = "Mapped XTM translation XLIFF with segment statuses copied from the target file.")]
    public FileReference File { get; set; } = default!;
}
