using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Files;

public class DownloadTranslatedInteroperableFileRequest
{
    [Display("Job ID", Description = "The job created by Upload interoperable source file for the desired target language.")]
    public string JobId { get; set; } = string.Empty;
}
