using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public abstract class XtmFileDescription
{
    [Display("Filename")]
    public string FileName { get; set; }
    
    [Display("File ID")]
    public string FileId { get; set; }
}

public class XtmProjectFileDescription : XtmFileDescription
{
    [Display("Job ID")]
    public string JobId { get; set; }
    
    [Display("Target language")]
    public string TargetLanguage { get; set; }
}

public class XtmSourceFileDescription : XtmFileDescription
{
    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; }
}