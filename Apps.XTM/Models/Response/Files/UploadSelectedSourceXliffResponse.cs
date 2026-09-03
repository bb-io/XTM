using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public class UploadSelectedSourceXliffResponse
{
    public string Name { get; set; } = string.Empty;

    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    public JobResponse[] Jobs { get; set; } = [];

    [Display("Prepared XLIFF file")]
    public FileReference File { get; set; } = new();

    [Display("Uploaded to XTM")]
    public bool Uploaded { get; set; }

    [Display("Segments excluded")]
    public int SegmentsExcluded { get; set; }

    [Display("Segments total")]
    public int SegmentsTotal { get; set; }

    [Display("Segments left")]
    public int SegmentsLeft { get; set; }

    [Display("Approximate word count")]
    public int ApproximateWordCount { get; set; }
}
