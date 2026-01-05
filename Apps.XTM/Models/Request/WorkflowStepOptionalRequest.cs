using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request;

public class WorkflowStepOptionalRequest
{
    [Display("Worflow step", Description =
        "Event will be triggered when the workflow transitions to the selected step. " +
        "Specify the project ID to select specific workflow steps for a project")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string? WorkflowStep { get; set; }
}
