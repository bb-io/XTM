using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Workflows;

public class MoveJobsToNextStepRequest
{
    [Display("Job IDs")]
    public List<string> JobIds { get; set; }

    [Display("Current workflow step"), DataSource(typeof(WorkflowStepDataHandler))]
    public string? CurrentWorkflowStep { get; set; }
}
