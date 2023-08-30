using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Customers;

public class ManageCustomersResponse
{
    [Display("Customer ID")]
    public string Id { get; set; }
    public string Name { get; set; }
}