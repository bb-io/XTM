using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request;

public class WorkflowStepOptionalRequest
{
    [Display("Workflow step", Description =
        "Event will be triggered when the workflow transitions to the selected step, or when it finishes if the finished-step option is enabled. " +
        "Specify the project ID to select specific workflow steps for a project")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string? WorkflowStep { get; set; }

    [Display("Trigger when workflow step is finished", Description =
        "Trigger when the selected workflow step is finished instead of when it becomes active or is reopened. Defaults to off.")]
    public bool? TriggerWhenStepFinished { get; set; }
}
