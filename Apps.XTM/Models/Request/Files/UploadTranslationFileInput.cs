using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class UploadTranslationFileInput
{
    [Display("File type")]
    [DataSource(typeof(FileTypeDataHandler))]
    public string FileType { get; set; }

    [Display("Job ID")]
    public string JobId { get; set; }

    [Display("Workflow step name")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string WorkflowStepName { get; set; }

    public Blackbird.Applications.Sdk.Common.Files.File File { get; set; }

    public string? Name { get; set; }

    [Display("Enable autopopulation?")] 
    public bool Autopopulation { get; set; }

    [Display("Segment status approving")]
    [DataSource(typeof(SegmentStatusApprovingDataHandler))]
    public string SegmentStatusApproving { get; set; }
}