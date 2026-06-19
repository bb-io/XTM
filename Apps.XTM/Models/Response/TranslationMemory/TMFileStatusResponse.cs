using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.TranslationMemory;

public class TMFileStatusResponse
{
    [Display("Message")]
    [JsonProperty("message")]
    public string? Message { get; set; }

    [Display("Status")]
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
}
