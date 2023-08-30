using Apps.XTM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Customers;

public class UpdateCustomerRequest
{
    public string Name { get; set; }
    
    public string? Nickname { get; set; }
    
    [Display("Project manager ID")]
    [JsonConverter(typeof(StringToIntConverter), nameof(ProjectManagerId))]
    public string? ProjectManagerId { get; set; }
    
    [Display("Project watchers IDs")] 
    [JsonConverter(typeof(StringToIntConverter), nameof(ProjectWatchers))]
    public IEnumerable<string>? ProjectWatchers { get; set; }
    
    [Display("Vat number")] public string? VatNumber { get; set; }
}