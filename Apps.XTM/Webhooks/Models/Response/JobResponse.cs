using Apps.XTM.Webhooks.Models.Payload;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Response;

public class JobResponse
{
    [Display("File name")] public string Filename { get; set; }

    [Display("Target language")] public string TargetLanguage { get; set; }

    [Display("Job ID")] public string JobId { get; set; }

    [Display("Source file ID")] public string SourceFileId { get; set; }

    public string Status { get; set; }

    public JobResponse(JobPayload payload)
    {
        Filename = payload.Filename;
        TargetLanguage = payload.TargetLanguage;
        JobId = payload.JobDescriptor.Id;
        SourceFileId = payload.SourceFileId;
        Status = payload.Status;
    }
}