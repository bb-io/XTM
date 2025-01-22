using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectAnalysis
{
    [Display("Project ID")]
    [JsonProperty("projectId")]
    public string Id { get; set; }

    [JsonProperty("activity")]
    public string Activity { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}