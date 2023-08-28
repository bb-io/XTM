using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Models.Request.Projects;

public class UploadTranslationFileInput
{
    [Display("File type")] public string FileType { get; set; }
    [Display("Job id")] public int JobId { get; set; }
    [Display("Workflow step name")] public string WorkflowStepName { get; set; }
    public File File { get; set; }
    public string? Name { get; set; }
    [Display("Enable autopopulation?")] public bool Autopopulation { get; set; }
    
    [Display("Segment status approving")] 
    [DataSource(typeof(SegmentStatusApprovingDataHandler))]
    public string SegmentStatusApproving { get; set; }
}