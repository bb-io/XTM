namespace Apps.XTM.Webhooks.Models.Payload;

public class WorkflowTransitionPayload
{
    public Descriptor ProjectDescriptor { get; set; }
    public List<EventPayload> Events { get; set; }
}