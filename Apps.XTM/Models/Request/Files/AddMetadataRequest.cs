using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.Files;

public class AddMetadataRequest
{
    [Display("Translated file")]
    public FileReference File { get; set; } = new();

    [Display("Job ID")]
    public string JobId { get; set; } = string.Empty;

    [Display("Attribute segments to user")]
    [StaticDataSource(typeof(SegmentAttributionDataSourceHandler))]
    public string AttributeSegmentsToUser { get; set; } = SegmentAttributionDataSourceHandler.OnlyConfirmed;

    [Display("Provenance type", Description = "Overrides workflow-based provenance type inference for every unit")]
    [StaticDataSource(typeof(ProvenanceTypeDataSourceHandler))]
    public string? ProvenanceType { get; set; }

    [Display("Workflow step", Description = "Workflow step whose assigned user should be used for person provenance. Defaults to the last non-automatic step.")]
    [DataSource(typeof(ManualWorkflowStepDataHandler))]
    public string? WorkflowStep { get; set; }
}
