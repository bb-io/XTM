using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.XTM.Models.Request.Workflows;

public class UpdateDueDatesRequest
{
    [Display("Type")]
    [StaticDataSource(typeof(DueDateTypeDataSourceHandler))]
    public string Type { get; set; } = string.Empty;

    [Display("Date", Description = "Date in UTC. Workflow start date must be earlier than the preprocessing due date when set.")]
    public DateTime Date { get; set; }

    [Display("Target identifier", Description = "Reference step name, job ID, or target language code. Leave empty for project and workflow dates.")]
    public string? TargetIdentifier { get; set; }
}
