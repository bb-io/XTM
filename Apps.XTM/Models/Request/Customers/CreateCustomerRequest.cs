using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Customers;

public class CreateCustomerRequest
{
    public string Name { get; set; }
    public string? Nickname { get; set; }
    [Display("TM and Terminology only")] public bool? TmAndTermOnly { get; set; }
    [Display("Project manager id")] public int? ProjectManagerId { get; set; }
    [Display("Project watchers ids")] public IEnumerable<int>? ProjectWatcherIds { get; set; }
}