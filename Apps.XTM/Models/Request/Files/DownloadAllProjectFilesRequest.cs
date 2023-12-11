using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class DownloadAllProjectFilesRequest
{
    [Display("File scope")]
    [DataSource(typeof(FileScopeDataHandler))]
    public string FileScope { get; set; }
    
    [Display("File type")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; }
    
    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
    
    [Display("Job IDs")]
    public IEnumerable<string>? JobIds { get; set; }
}