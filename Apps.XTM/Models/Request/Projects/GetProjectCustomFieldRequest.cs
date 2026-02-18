using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class GetProjectCustomFieldRequest
{
    [Display("Custom field definition ID")]
    [DataSource(typeof(ProjectCustomFieldDataHandler))]
    public String DefinitionId { get; set; }
}
