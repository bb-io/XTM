using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows;

public class WorkflowStepResponse
{
    [Display("Workflow step ID")]
    public string Id { get; set; }
    
    public string Name { get; set; }
}