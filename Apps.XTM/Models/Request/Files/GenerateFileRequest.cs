using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Files;

public class GenerateFileRequest
{
    [Display("File type")]
    [JsonProperty("fileType")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; }
    
    [Display("Target language")]
    [JsonProperty("targetLanguage")]
    [DataSource(typeof(ProjectTargetLanguageDataSourceHandler))]
    public string? TargetLanguage { get; set; }
}