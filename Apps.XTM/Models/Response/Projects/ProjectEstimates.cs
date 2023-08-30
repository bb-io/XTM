using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectEstimates
{
    [Display("Project ID")] public string ProjectId { get; set; }

    public double Price { get; set; }

    [Display("Tax price")] public double TaxPrice { get; set; }

    [Display("Delivery date in UNIX")] public long DeliveryDate { get; set; }

    public string Currency { get; set; }
}