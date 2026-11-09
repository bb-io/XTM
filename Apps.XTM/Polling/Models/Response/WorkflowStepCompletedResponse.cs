using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Polling.Models.Response;

public class WorkflowStepCompletedResponse
{
    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Workflow step")]
    public string WorkflowStep { get; set; } = string.Empty;

    [Display("Target language")]
    public string? TargetLanguage { get; set; }

    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];
}
