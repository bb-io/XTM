using Apps.XTM.Webhooks.Models.Payload;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Response;

public class TaskResponse
{
    [Display("Current user")]
    public UserPayload CurrentUser { get; set; }
    
    [Display("File name")]
    public string FileName { get; set; }
    
    [Display("Target language")]
    public string TargetLanguage { get; set; }
    
    [Display("Workflow step name")]
    public string WorkflowStepName { get; set; }
    
    [Display("Workflow step")]
    public string WorkflowStep { get; set; }
    
    [Display("Job ID")]
    public string JobId { get; set; }
    public TaskResponse(TaskPayload payload)
    {
        CurrentUser = payload.CurrentUser;
        FileName = payload.Filename;
        TargetLanguage = payload.TargetLanguage;
        WorkflowStepName = payload.Step.WorkflowStepName;
        WorkflowStep = payload.Step.WorkflowStep;
        JobId = payload.Job.Id;
    }
}