namespace Apps.XTM.Models.Response.Files;

public class GetProjectFilesResponse
{
    public IEnumerable<FileWithData> Files { get; set; }
}