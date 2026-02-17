using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.TranslationMemory;

public class TranslationMemoryPenaltyProfileResponse
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }
}
