using Newtonsoft.Json;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectCompletionResponse
{
    [Display("Activity")]
    public string Activity { get; set; }

    [Display("Jobs")]
    public List<ProjectCompletionJobResponse> Jobs { get; set; }

    [Display("Job IDs")]
    [JsonIgnore]
    public IEnumerable<string> JobIds { get; set; }
}
