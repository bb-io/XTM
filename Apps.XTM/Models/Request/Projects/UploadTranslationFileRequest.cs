namespace Apps.XTM.Models.Request.Projects;

public class UploadTranslationFileRequest
{
    public string FileType { get; set; }
    public int JobId { get; set; }
    public string WorkflowStepName { get; set; }
    public TranslationFile TranslationFile { get; set; }
    public XliffOptions XliffOptions { get; set; }
}