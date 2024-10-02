using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public class UploadTranslationFileResponse
{
    [Display("File ID")]
    public string FileId { get; set; } = string.Empty;
    
    [Display("Job ID")]
    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}