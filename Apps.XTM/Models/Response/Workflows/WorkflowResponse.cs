using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows;

public class WorkflowResponse
{
    [Display("Workflow ID")]
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public WorkflowStepResponse[] Steps { get; set; }
}