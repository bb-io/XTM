using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class CreateProjectResponse
{
    public string Name { get; set; }
    [Display("Project ID")] public string ProjectId { get; set; }
    public JobResponse[] Jobs { get; set; }
}