using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public class UploadStatusResponse
{
    [Display("File ID")]
    public string FileId { get; set; } = string.Empty;
    
    [Display("Error Description")]
    public string ErrorDescription { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
}