using Apps.XTM.Webhooks.Models.Payload;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Response;

public class AnalysisFinishedResponse
{
    [Display("Project manager")]
    public UserPayload ProjectManager { get; set; }
    
    public UserPayload Creator { get; set; }
    
    public string Activity { get; set; }
    
    [Display("Project name")]
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public string Status { get; set; }
    
    public IEnumerable<JobResponse> Jobs { get; set; }
    
    [Display("Source language")]
    public string SourceLanguage { get; set; }
    
    [Display("Target languages")]
    public IEnumerable<string> TargetLanguages { get; set; }
    
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("TM customers")]
    public IEnumerable<UserPayload> TmCustomers { get; set; }
    
    [Display("Term customers")]
    public IEnumerable<UserPayload> TermCustomers { get; set; }
    
    public UserPayload Customer { get; set; }
    
    public AnalysisFinishedResponse(AnalysisFinishedPayload payload)
    {
        ProjectManager = payload.ProjectManager;
        Creator = payload.Creator;
        Activity = payload.Activity;
        Name = payload.Name;
        Description = payload.Description;
        Status = payload.Status;
        SourceLanguage = payload.SourceLanguage;
        TargetLanguages = payload.TargetLanguages;
        ProjectId = payload.ProjectDescriptor.Id;
        TmCustomers = payload.TmCustomers;
        TermCustomers = payload.TermCustomers;
        Customer = payload.Customer;
        Jobs = payload.Jobs.Select(x => new JobResponse(x));
    }
}