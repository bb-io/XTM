using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request;

public class WorkflowStepOptionalRequest
{
    [Display("Workflow step", Description =
        "Event will be triggered when the workflow transitions to the selected step. " +
        "Specify the project ID to select specific workflow steps for a project")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string? WorkflowStep { get; set; }

    [Display("Ignore reopened steps", Description =
        "Trigger only when a task is activated, including the first workflow step. " +
        "Ignore notifications that a task was reopened. Defaults to off.")]
    public bool? IgnoreReopenedSteps { get; set; }
}
