using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Tag;

public class TagResponse
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }
}
