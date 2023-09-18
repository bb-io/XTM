using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Models.Response.Files;

public class FileWithData
{
    public File Content { get; set; }
    public string Name { get; set; }
}