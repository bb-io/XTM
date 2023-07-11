using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Projects;

public class UploadSourceFileRequest
{
    [JsonProperty("file")]
    public byte[] File { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
}