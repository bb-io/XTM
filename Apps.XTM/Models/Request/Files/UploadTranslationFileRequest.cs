using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.Files;

public class UploadTranslationFileRequest
{
    [Display("Job ID")]
    public string JobId { get; set; } = string.Empty;

    public FileReference File { get; set; } = new();

    [Display("File type")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; } = string.Empty;

    [Display("File name overwrite", Description = "Optional. If not provided, the original file name will be used.")]
    public string? Name { get; set; }

    [Display("Segment status approving", Description = "By default segment status will be updated accordingly to state.")]
    [DataSource(typeof(SegmentStatusApprovingDataHandler))]
    public string? SegmentStatusApproving { get; set; }

    [Display("Enable autopopulation?", Description = "By default autopopulation is enabled.")] 
    public bool? Autopopulation { get; set; }

    [Display("Workflow step name", Description = "Deprecated, only needed when autopopulation is disabled.")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string? WorkflowStepName { get; set; }
}