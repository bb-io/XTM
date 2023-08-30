namespace Apps.XTM.Webhooks.Models.Payload;

public class AnalysisFinishedPayload
{
    public UserPayload ProjectManager { get; set; }
    
    public UserPayload Creator { get; set; }
    
    public string Activity { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public string Status { get; set; }
    
    public IEnumerable<JobPayload> Jobs { get; set; }
    
    public string SourceLanguage { get; set; }
    
    public IEnumerable<string> TargetLanguages { get; set; }
    
    public Descriptor ProjectDescriptor { get; set; }
    
    public IEnumerable<UserPayload> TmCustomers { get; set; }
    
    public IEnumerable<UserPayload> TermCustomers { get; set; }
    
    public UserPayload Customer { get; set; }
}