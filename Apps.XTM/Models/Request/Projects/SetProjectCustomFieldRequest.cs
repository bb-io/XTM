using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class SetProjectCustomFieldRequest
{
   
    [Display("Custom field ID")]
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
