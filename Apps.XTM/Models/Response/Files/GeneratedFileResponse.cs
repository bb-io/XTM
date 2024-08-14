using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public class GeneratedFileResponse
{
    [Display("File ID")]
    public string FileId { get; set; }

    [Display("Job ID")]
    public string JobId { get; set; }

    [Display("File type")]
    public string FileType { get; set; }
}