using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class ProjectAcceptedPayload
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("External project ID")]
    public string ExternalProjectId { get; set; }
    
    [Display("UUID")]
    public string Uuid { get; set; }
}