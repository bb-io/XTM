using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class GenerateFileRequest
{
    [Display("File type")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; } = string.Empty;

    [Display("Target language")]
    [DataSource(typeof(ProjectTargetLanguageDataSourceHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Job IDs")]
    public IEnumerable<string>? JobIds { get; set; }

    [Display("Active workflow steps")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public IEnumerable<string>? ActiveWorkflowSteps { get; set; }

    [Display("Include in extended table")]
    [DataSource(typeof(ExtendedTablePropertiesDataSourceHandler))]
    public IEnumerable<string>? PropertiesToInclude { get; set; }

}