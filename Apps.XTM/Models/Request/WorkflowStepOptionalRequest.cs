using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request
{
    public class WorkflowStepOptionalRequest
    {
        [Display("Worflow step")]
        [DataSource(typeof(WorkflowStepDataHandler))]
        public string? WorkflowStep { get; set; }
    }
}
