using Apps.XTM.DataSourceHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class CreateProjectFromTemplateRequest : CustomerRequest
{
    public string Name { get; set; }

    public string? Description { get; set; }

    [Display("Template ID")]
    [DataSource(typeof(ProjectTemplateDataHandler))]
    public string TemplateId { get; set; }

    [Display("Due date")]
    public DateTime? DueDate { get; set; }

    [Display("Project created callback URL")]
    public string? ProjectCreatedCallback { get; set; }

    [Display("Project accepted callback URL")]
    public string? ProjectAcceptedCallback { get; set; }

    [Display("Project finished callback URL")]
    public string? ProjectFinishedCallback { get; set; }

    [Display("Job finished callback URL")]
    public string? JobFinishedCallback { get; set; }

    [Display("Analysis finished callback URL")]
    public string? AnalysisFinishedCallback { get; set; }

    [Display("Workflow transition callback URL")]
    public string? WorkflowTransitionCallback { get; set; }

    [Display("Invoice status changed callback URL")]
    public string? InvoiceStatusChangedCallback { get; set; }
}