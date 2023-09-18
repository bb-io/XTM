﻿using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Files;

public class DownloadAllProjectFilesRequest
{
    [Display("File scope")]
    [JsonProperty("fileScope")]
    [DataSource(typeof(FileScopeDataHandler))]
    public string FileScope { get; set; }
    
    [Display("File type")]
    [JsonProperty("fileType")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; }
    
    [Display("Target language")]
    [JsonProperty("targetLanguage")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? TargetLanguage { get; set; }

}