using Blackbird.Applications.Sdk.Common;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Models.Response.Files;

public class FileWithData<T> where T : XtmFileDescription
{
    public File Content { get; set; }

    [Display("File description")]
    public T FileDescription { get; set; }
}