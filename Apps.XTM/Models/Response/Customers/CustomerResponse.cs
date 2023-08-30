using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Customers;

public class CustomerResponse
{
    [Display("Customer ID")]
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    [Display("Project manager ID")] public string ProjectManagerId { get; set; }
}