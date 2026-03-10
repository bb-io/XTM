using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request;

public class WorkflowTransitionPollingRequest
{
    [Display("Workflow steps", Description = "Workflow steps to monitor for job transitions. The event triggers when new jobs appear in these steps.")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public IEnumerable<string> WorkflowSteps { get; set; } = [];

    [Display("Customer IDs")]
    [DataSource(typeof(CustomerDataHandler))]
    public IEnumerable<string>? CustomerIds { get; set; }

    [Display("Project IDs")]
    public IEnumerable<string>? ProjectIds { get; set; }
}
