using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectEstimates
{
    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string ProjectId { get; set; }

    [JsonProperty("price")]
    public double Price { get; set; }

    [Display("Tax price")]
    [JsonProperty("taxPrice")]
    public double TaxPrice { get; set; }

    [Display("Delivery date in UNIX")]
    [JsonProperty("deliveryDate")]
    public long? DeliveryDate { get; set; }

    public DateTime? DeliveryDateFormatted =>
            DeliveryDate is > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(DeliveryDate.Value).UtcDateTime
                : (DateTime?)null;

    [JsonProperty("currency")]
    public string Currency { get; set; }
}