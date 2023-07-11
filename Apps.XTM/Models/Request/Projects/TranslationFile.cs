using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Projects;

public class TranslationFile
{
    [JsonProperty("file")]
    public byte[] File { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
}