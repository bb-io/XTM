using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class SearchProjectJobsResponse
{
    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];
}
