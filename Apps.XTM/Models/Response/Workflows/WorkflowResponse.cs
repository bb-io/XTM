namespace Apps.XTM.Models.Response.Workflows;

public class WorkflowResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public WorkflowStepResponse[] Steps { get; set; }
}