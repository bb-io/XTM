using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class JobFinishedPayload
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("Customer ID")]
    public string CustomerId { get; set; }
    
    [Display("Job ID")]
    public string JobId { get; set; }
    
    [Display("UUID")]
    public string Uuid { get; set; }
}