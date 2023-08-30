
namespace Apps.XTM.Webhooks.Models.Payload;

public class JobPayload
{
    public string Filename { get; set; }
    
    public string TargetLanguage { get; set; }
    
    public Descriptor JobDescriptor { get; set; }
    
    public string SourceFileId { get; set; }
    
    public string Status { get; set; }
}