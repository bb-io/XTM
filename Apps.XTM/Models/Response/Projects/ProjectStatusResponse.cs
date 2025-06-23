using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectStatusResponse
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    [Display("Completion status")]
    public string CompletionStatus { get; set; }
    public string Activity { get; set; }
    [Display("Source language")]
    public string SourceLanguage { get; set; }
    [Display("Due Date")]
    public long? DueDate { get; set; }
    [Display("Join files type")]
    public string? JoinFilesType { get; set; }
    [Display("Contractor type")]
    public string? ContractorType { get; set; }
}