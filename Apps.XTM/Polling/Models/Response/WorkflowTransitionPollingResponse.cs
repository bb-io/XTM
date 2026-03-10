using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Polling.Models.Response;

public class WorkflowTransitionPollingResponse
{
    [Display("Projects")]
    public IEnumerable<WorkflowTransitionJobItem> Projects { get; set; } = [];
}
