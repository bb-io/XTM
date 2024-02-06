using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Templates;

public class ProjectTemplate : SimpleProjectTemplateResponse
{
    [Display("Customer ID")]
    public string CustomerId { get; set; }

    [Display("Workflow ID")]
    public string WorkflowId { get; set; }
}