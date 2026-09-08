using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class DownloadTranslatedInteroperableFileRequest
{
    [Display("Job ID", Description = "The job created by Upload interoperable source file for the desired target language.")]
    public string JobId { get; set; } = string.Empty;

    [Display("Attribute segments to user", Description = "Attribute segments to the selected workflow step's assignee. This is a workflow attribution policy, not XTM editor history. Defaults to only confirmed segments.")]
    [StaticDataSource(typeof(SegmentAttributionDataSourceHandler))]
    public string AttributeSegmentsToUser { get; set; } = SegmentAttributionDataSourceHandler.OnlyConfirmed;

    [Display("Provenance type", Description = "Override translation or review provenance. Otherwise use the selected workflow step's role.")]
    [StaticDataSource(typeof(ProvenanceTypeDataSourceHandler))]
    public string? ProvenanceType { get; set; }

    [Display("Workflow step", Description = "Manual workflow step whose assignees receive attribution. Defaults to the latest active or finished manual step.")]
    [DataSource(typeof(ManualWorkflowStepDataHandler))]
    public string? WorkflowStep { get; set; }
}
