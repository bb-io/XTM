using Apps.XTM.DataSourceHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class CreateProjectFromTemplateRequest : CustomerRequest
{
    public string Name { get; set; }

    public string Description { get; set; }

    [Display("Template")]
    [DataSource(typeof(ProjectTemplateDataHandler))]
    public string TemplateId { get; set; }
}