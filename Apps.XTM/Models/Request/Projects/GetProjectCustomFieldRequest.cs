using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class GetProjectCustomFieldRequest
{
    [Display("Custom field definition ID")]
    public String DefinitionId { get; set; }
}
