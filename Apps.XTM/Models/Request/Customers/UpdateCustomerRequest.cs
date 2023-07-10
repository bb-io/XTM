using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Customers;

public class UpdateCustomerRequest
{
    public string Name { get; set; }
    public string? Nickname { get; set; }
    [Display("Project manager id")] public int? ProjectManagerId { get; set; }
    [Display("Project watchers")] public IEnumerable<int>? ProjectWatchers { get; set; }
    [Display("Vat number")] public string? VatNumber { get; set; }
}