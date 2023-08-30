namespace Apps.XTM.Webhooks.Models.Payload;

public class WorkflowTransitionPayload
{
    public Descriptor ProjectDescriptor { get; set; }
    public IEnumerable<EventPayload> Events { get; set; }
}