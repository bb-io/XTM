namespace Apps.XTM.Webhooks.Models.Payload;

public class TaskPayload
{
    public UserPayload CurrentUser { get; set; }
    public string Filename { get; set; }
    public string TargetLanguage { get; set; }
    public WorkflowStepPayload Step { get; set; }
    public Descriptor Job { get; set; }
}