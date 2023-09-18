using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class ProjectCreatedPayload
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("UUID")]
    public string Uuid { get; set; }
}