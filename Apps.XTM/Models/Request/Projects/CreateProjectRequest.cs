using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class CreateProjectRequest
{
    [Display("Name")]
    public string Name { get; set; }
    
    [Display("Workflow")] 
    [DataSource(typeof(WorkflowDataHandler))]
    public string WorkflowId { get; set; }
        
    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string SourceLanguage { get; set; }
    
    [Display("Target languages")]
    [DataSource(typeof(LanguageDataHandler))]
    public IEnumerable<string> TargetLanguages { get; set; }

    [Display("Due date")]
    public DateTime? DueDate { get; set; }

    [Display("Description")]
    public string? Description { get; set; }
        
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

    [Display("Template ID"), DataSource(typeof(ProjectTemplateDataHandler))]
    public string? ProjectTemplateId { get; set; }
}