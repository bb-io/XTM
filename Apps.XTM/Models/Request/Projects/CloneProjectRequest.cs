using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class CloneProjectRequest
{
    public string? Name { get; set; }
    [Display("Origin id")] public int OriginId { get; set; }
}