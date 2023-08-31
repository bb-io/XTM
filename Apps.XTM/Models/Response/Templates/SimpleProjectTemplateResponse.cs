using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Templates;

public class SimpleProjectTemplateResponse
{
    [Display("Template ID")]
    public string Id { get; set; }
    
    public string Name { get; set; }
}