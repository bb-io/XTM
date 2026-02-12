using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public class ListGeneratedFilesResponse
{
    public GeneratedFileResponse[] Files { get; set; }

    [Display("Job IDs")]
    public IEnumerable<string> JobIds => Files?.Select(f => f.JobId) ?? [];

    public ListGeneratedFilesResponse(GeneratedFileResponse[] files)
    {
        Files = files;
    }

    public ListGeneratedFilesResponse()
    {
        Files = [];
    }
}