using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class InvoiceStatusChangedPayload
{
    [Display("Project ID")]
    public string ProjectId { get; set; }
    
    [Display("Invoice status")]
    public string InvoiceStatus { get; set; }
    
    [Display("UUID")]
    public string Uuid { get; set; }
}