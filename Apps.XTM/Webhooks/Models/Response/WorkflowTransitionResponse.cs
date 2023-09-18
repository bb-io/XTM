using Apps.XTM.Webhooks.Models.Payload;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Response;

public class WorkflowTransitionResponse
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("Customer ID")]
    public string CustomerId { get; set; }
    
    public IEnumerable<EventResponse> Events { get; set; }

    public WorkflowTransitionResponse(WorkflowTransitionPayload payload)
    {
        ProjectId = payload.ProjectDescriptor.Id;
        Events = payload.Events.Select(x => new EventResponse(x));
    }
}