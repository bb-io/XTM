using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Tag;

public class TagGroupResponse
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }
}
