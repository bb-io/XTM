using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Customers;

public class CustomerResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    [Display("Project manager id")] public int ProjectManagerId { get; set; }
}