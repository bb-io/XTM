using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class DownloadProjectFileRequest
{
    [Display("File scope")]
    [DataSource(typeof(FileScopeDataHandler))]
    public string FileScope { get; set; }
    
    [Display("File ID")]
    public string FileId { get; set; }
}