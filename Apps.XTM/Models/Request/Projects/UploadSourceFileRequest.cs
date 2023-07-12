using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Projects;

public class UploadSourceFileRequest
{
    [JsonProperty("file")]
    public byte[] File { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [Display("Target languages")] public IEnumerable<string>? TargetLanguages { get; set; }
    [Display("Workflow id")] public int? WorkflowId { get; set; }
    [Display("Tag ids")] public IEnumerable<int>? TagIds { get; set; }
    [Display("Translation type")] public string? TranslationType { get; set; }
    public string? Metadata { get; set; }
    [Display("Metadata type")] public string? MetadataType { get; set; }
}