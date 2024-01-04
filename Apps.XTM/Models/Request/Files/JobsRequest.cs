using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Files;

public class JobsRequest
{
    [Display("Job IDs")]
    public IEnumerable<string>? JobIds { get; set; }
}