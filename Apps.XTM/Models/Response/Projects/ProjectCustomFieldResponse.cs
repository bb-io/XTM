using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectCustomFieldResponse
{
    [Display("Custom field ID")]
    public string Id { get; set; }

    [Display("Project ID")]
    public string ProjectId { get; set; }

    [Display("Definition ID")]
    public string DefinitionId { get; set; }
    public string Name { get; set; }
    public ProjectCustomFieldValue? Value { get; set; }
}

public class ProjectCustomFieldValue
{
    [Display("Multiselect IDs")]
    public List<string>? Ids { get; set; }

    [Display("Text value")]
    public string? Value { get; set; }
    public string? Date { get; set; }

    [Display("Boolean value")]
    public bool? BooleanValue { get; set; }
}
