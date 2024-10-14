using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Invoices;

public class InvoiceOptionalRequest
{
    [Display("Invoice ID")]
    public string? InvoiceId { get; set; }
}