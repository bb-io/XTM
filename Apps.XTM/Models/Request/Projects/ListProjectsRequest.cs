using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class ListProjectsRequest
{
    [Display("Project name")]
    public string? Name { get; set; }
}