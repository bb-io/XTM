using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Jobs;

public class SearchProjectJobsRequest
{
    [Display("Completed steps", Description = "Only include jobs that have finished every selected workflow step.")]
    [DataSource(typeof(WorkflowStepDataHandler))]
    public IEnumerable<string>? CompletedSteps { get; set; }

    [Display("Target languages")]
    [DataSource(typeof(ProjectTargetLanguageDataSourceHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
}
