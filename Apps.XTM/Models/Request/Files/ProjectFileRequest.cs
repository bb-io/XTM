using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class ProjectFileRequest
{
    [Display("Project file ID"), DataSource(typeof(ProjectFileDataHandler))]
    public string ProjectFileId { get; set; } = string.Empty;
}