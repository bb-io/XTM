using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public class FileWithData<T> where T : XtmFileDescription
{
    public FileReference Content { get; set; }

    [Display("File description")]
    public T FileDescription { get; set; }
}