using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request;

public class WorkflowStepCompletedPollingRequest
{
    [Display("Workflow step", Description = "Trigger when every job in the selected scope has finished this step.")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public string WorkflowStep { get; set; } = string.Empty;

    [Display("Project IDs", Description = "Monitor these active projects. Leave empty to monitor all active projects.")]
    public IEnumerable<string>? ProjectIds { get; set; }

    [Display("Project name contains", Description = "Only monitor active projects whose names contain this text. Case-sensitive.")]
    public string? ProjectNameContains { get; set; }

    [Display("Completion scope", Description = "Wait for the entire project, or trigger separately for each target language. Defaults to Entire project.")]
    [StaticDataSource(typeof(CompletionScopeDataHandler))]
    public string? CompletionScope { get; set; }
}
