using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.TranslationMemory;

public class TagTmSegmentsBasedOnEditsResponse
{
    [Display("Export file ID")]
    public string ExportFileId { get; set; } = string.Empty;

    [Display("Filtered TMX file")]
    public FileReference? FilteredTmxFile { get; set; }

    [Display("Exported segments scanned")]
    public int ExportedSegmentsScanned { get; set; }

    [Display("Segments matched")]
    public int SegmentsMatched { get; set; }

    [Display("Segments skipped")]
    public int SegmentsSkipped { get; set; }

    [Display("Imported file ID")]
    public string? ImportedFileId { get; set; }

    [Display("Imported file name")]
    public string? ImportedFileName { get; set; }

    [Display("Import status")]
    public string ImportStatus { get; set; } = string.Empty;
}
