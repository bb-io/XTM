using Apps.XTM.Webhooks.Models.Payload;

namespace Apps.XTM.Webhooks.Models.Response;

public class EventResponse
{
    public string Type { get; set; }
    public IEnumerable<TaskResponse> Tasks { get; set; }

    public EventResponse(EventPayload payload)
    {
        Type = payload.Type;
        Tasks = payload.Tasks.Select(x => new TaskResponse(x));
    }
}