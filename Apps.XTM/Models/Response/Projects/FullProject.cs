using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class FullProject
{
    [Display("Project ID")] public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Activity { get; set; }

    [Display("Customer name")] public string CustomerName { get; set; }

    [Display("Source language")] public string SourceLanguage { get; set; }

    [Display("Target languages")] public IEnumerable<string> TargetLanguages { get; set; }
}