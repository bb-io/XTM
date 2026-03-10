using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Polling.Models.Response;

public class WorkflowTransitionJobItem
{
    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];
}
