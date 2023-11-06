namespace Apps.XTM.Webhooks.Models.Payload;

public class BridgeWebhookPayload<T>
{
    public Dictionary<string, string> Parameters { get; set; }
    public T Payload { get; set; }
}