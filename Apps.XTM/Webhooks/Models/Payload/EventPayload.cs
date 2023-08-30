namespace Apps.XTM.Webhooks.Models.Payload;

public class EventPayload
{
    public string Type { get; set; }
    public IEnumerable<TaskPayload> Tasks { get; set; }
}