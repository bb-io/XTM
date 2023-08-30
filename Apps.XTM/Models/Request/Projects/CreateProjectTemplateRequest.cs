using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class CreateProjectTemplateRequest
{
    [Display("Source project ID")]
    [DataSource(typeof(ProjectDataHandler))]
    public string SourceProjectId { get; set; }
    
    [Display("TemplateType")]
    [DataSource(typeof(TemplateTypeDataHandler))]
    public string TemplateType { get; set; }
    
    public string Name { get; set; }
    
    public string? Description { get; set; }
    
    [Display("Customer ID")]
    [DataSource(typeof(CustomerDataHandler))]
    public string? CustomerId { get; set; }
}