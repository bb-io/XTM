using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.TranslationMemory;

public class GenerateTmxFromXliffRequest
{
    [Display("Content")]
    public FileReference File { get; set; }

    [Display("Tag to assign")]
    public string? Tag { get; set; }

    [Display("Tag group")]
    public string? TagGroup { get; set; }
}
