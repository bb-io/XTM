using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class UploadTranslationFileInput
{
    [Display("File type")] public string FileType { get; set; }
    [Display("Job id")] public int JobId { get; set; }
    [Display("Workflow step name")] public string WorkflowStepName { get; set; }
    public byte[] File { get; set; }
    public string? Name { get; set; }
    [Display("Enable autopopulation?")] public bool Autopopulation { get; set; }
    [Display("Segment status approving")] public string SegmentStatusApproving { get; set; }
}