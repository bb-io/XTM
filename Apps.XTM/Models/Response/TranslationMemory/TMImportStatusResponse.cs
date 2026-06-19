using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.TranslationMemory;

public class TMImportStatusResponse
{
    [Display("File ID")]
    [JsonProperty("fileId")]
    public string FileId { get; set; } = string.Empty;

    [Display("Status")]
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [Display("Extraction error")]
    [JsonProperty("extractionError")]
    public string? ExtractionError { get; set; }

    [Display("Bilingual term extraction error")]
    [JsonProperty("bilingualTermExtractionError")]
    public string? BilingualTermExtractionError { get; set; }
}
