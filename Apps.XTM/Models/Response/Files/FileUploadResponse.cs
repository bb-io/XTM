using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Files;

public class FileUploadResponse
{
    [JsonProperty("file")]
    public FileJobResponse File { get; set; } = new();
}

public class FileJobResponse
{
    [Display("Job ID")]
    public string JobId { get; set; } = string.Empty;
    
    [Display("File ID")]
    public string FileId { get; set; } = string.Empty;
}