using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectDetailsResponse
{
    [Display("Project completion")]
    public ProjectCompletionResponse ProjectCompletion { get; set; }
    [Display("Project status")]
    public ProjectStatusResponse ProjectStatus { get; set; }
    [Display("Project estimates")]
    public ProjectEstimates ProjectEstimates { get; set; }
}