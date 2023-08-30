using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class SimpleProject
{
    [Display("Project ID")]
    public string Id { get; set; }
    public string Name { get; set; }
    
    public string Activity { get; set; }
    
    public string Status { get; set; }
}