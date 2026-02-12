using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public class DownloadFilesResponse<T> where T : XtmFileDescription
{
    [Display("Files with description")]
    public IEnumerable<FileWithData<T>> Files { get; set; }

    [Display("Files")]
    public IEnumerable<FileReference> RawFiles => Files?.Select(f => f.Content) ?? [];

    public DownloadFilesResponse(IEnumerable<FileWithData<T>> files)
    {
        Files = files;
    }

    public DownloadFilesResponse()
    {
        Files = [];
    }
}