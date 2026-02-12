using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public record DownloadFilesResponse<T>(
    [Display("Files with description")] IEnumerable<FileWithData<T>> Files
) where T : XtmFileDescription
{
    [Display("Files")]
    public IEnumerable<FileReference> RawFiles => Files.Select(f => f.Content);
}