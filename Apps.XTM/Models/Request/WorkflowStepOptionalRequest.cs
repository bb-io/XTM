using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request
{
    public class WorkflowStepOptionalRequest
    {
        [Display("Worflow step", Description = "Event will be triggered when workflow gets into a selected step.")]
        [DataSource(typeof(WorkflowStepDataHandler))]
        public string? WorkflowStep { get; set; }
    }
}
