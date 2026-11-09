using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Jobs;

public class JobRequest
{
    [Display("Job ID")]
    public string JobId { get; set; } = string.Empty;
}
