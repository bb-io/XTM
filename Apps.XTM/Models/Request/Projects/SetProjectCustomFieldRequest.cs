using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class SetProjectCustomFieldRequest
{
   
    [Display("Custom field definition ID")]
    [DataSource(typeof(ProjectCustomFieldDataHandler))]
    public string Id { get; set; }

    [Display("Text value")]
    public string? Value { get; set; }

    [Display("Date (ISO-8601)")]
    public string? Date { get; set; }

    [Display("Boolean value")]
    public bool? BooleanValue { get; set; }

    [Display("Multiselect IDs")]
    public List<string>? Ids { get; set; }
    
}
