using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Models.Request.Projects;

public class UploadSourceFileRequest
{
    [JsonProperty("file")]
    public File File { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
    [Display("Target languages")] public IEnumerable<string>? TargetLanguages { get; set; }
    
    [Display("Workflow")] 
    [DataSource(typeof(WorkflowDataHandler))]
    public string WorkflowId { get; set; }
    
    [Display("Tag ids")] public IEnumerable<int>? TagIds { get; set; }
    
    [Display("Translation type")]
    [DataSource(typeof(TranslationTypeDataHandler))]
    public string TranslationType { get; set; }
    
    public string? Metadata { get; set; }
    [Display("Metadata type")] public string? MetadataType { get; set; }
}