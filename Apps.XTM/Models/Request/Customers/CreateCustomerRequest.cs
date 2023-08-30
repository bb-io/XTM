using Apps.XTM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Customers;

public class CreateCustomerRequest
{
    public string Name { get; set; }
    
    public string? Nickname { get; set; }
    
    [Display("TM and Terminology only")] public bool? TmAndTermOnly { get; set; }
    
    [Display("Project manager ID")] 
    [JsonConverter(typeof(StringToIntConverter), nameof(ProjectManagerId))]
    public string? ProjectManagerId { get; set; }
    
    [Display("Project watchers IDs")]
    [JsonConverter(typeof(StringToIntConverter), nameof(ProjectWatcherIds))]
    public IEnumerable<string>? ProjectWatcherIds { get; set; }
}