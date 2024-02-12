using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response;

public class JobResponse
{
    [Display("Job ID")]
    public string JobId { get; set; }
    
    [Display("File name")]
    public string FileName { get; set; }
    
    [Display("Source file ID")]
    public string SourceFileId { get; set; }
    
    [Display("Source language")]
    public string SourceLanguage { get; set; }
    
    [Display("Target language")]
    public string TargetLanguage { get; set; }
    
    [Display("Join files type")]
    public string JoinFilesType { get; set; }

    [Display("Steps")]
    public List<StepResponse> Steps { get; set; }
}