using Newtonsoft.Json;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectCompletionStepResponse
{
    [Display("Status")]
    public string Status { get; set; }

    [Display("Step name")]
    [JsonProperty("displayStepName")]
    public string StepName { get; set; }

    [Display("Due to date")]
    public DateTime DueToDate { get; set; }
}
