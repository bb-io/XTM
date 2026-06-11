using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.TranslationMemory;

public class GenerateTmxFromXliffResponse
{
    [Display("TMX file")]
    public FileReference File { get; set; }

    [Display("Number of segments tagged")]
    public int SegmentsTagged { get; set; }

    [Display("Tag group used")]
    public string TagGroupUsed { get; set; }

    [Display("Tag used")]
    public string TagUsed { get; set; }
}
