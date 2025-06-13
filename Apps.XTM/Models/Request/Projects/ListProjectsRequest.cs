using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class ListProjectsRequest
{
    [Display("Project name")]
    public string? Name { get; set; }

    [StaticDataSource(typeof(ProjectStatusHandler))]
    public string? Status { get; set; }

    [Display("Created from")]
    public DateTime? CreatedFrom { get; set; }

    [Display("Created to")]
    public DateTime? CreatedTo { get; set; }

    [DataSource(typeof(ProjectActivityHandler))]
    public string? Activity { get; set; }

    [Display("Customer IDs")]
    [DataSource(typeof(CustomerDataHandler))]
    public IEnumerable<string>? CustomerIds { get; set; }

    [Display("Finished from")]
    public DateTime? FinishedFrom { get; set; }

    [Display("Finished to")]
    public DateTime? FinishedTo { get; set; }

    [Display("Modified from")]
    public DateTime? ModifiedFrom { get; set; }

    [Display("Modified to")]
    public DateTime? ModifiedTo { get; set; }

    [Display("Name must match exactly")]
    public bool? NameExactMatch { get; set; }

}