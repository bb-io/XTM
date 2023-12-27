using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Files;

public class UploadSourceFileRequest
{
    [JsonProperty("file")]
    public FileReference File { get; set; }
    
    [Display("Workflow")] 
    [DataSource(typeof(WorkflowDataHandler))]
    public string WorkflowId { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [Display("Target languages")] 
    [DataSource(typeof(ProjectTargetLanguageDataSourceHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
    
    [Display("Tag IDs")]
    [JsonConverter(typeof(StringToIntConverter), nameof(TagIds))]
    public IEnumerable<string>? TagIds { get; set; }
    
    [Display("Translation type")]
    [DataSource(typeof(TranslationTypeDataHandler))]
    public string? TranslationType { get; set; }
    
    [Display("Metadata in JSON format")]
    public string? Metadata { get; set; }
}