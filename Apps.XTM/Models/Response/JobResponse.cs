namespace Apps.XTM.Models.Response;

public class JobResponse
{
    public int JobId { get; set; }
    public string FileName { get; set; }
    public int SourceFileId { get; set; }
    public string SourceLanguage { get; set; }
    public string TargetLanguage { get; set; }
    public string JoinFilesType { get; set; }
}