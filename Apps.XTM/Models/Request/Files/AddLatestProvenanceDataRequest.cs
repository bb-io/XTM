using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.Files;

public class AddLatestProvenanceDataRequest
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Job ID", Description = "Recommended for projects containing several files in the same target language.")]
    [DataSource(typeof(ProjectJobDataHandler))]
    public string? JobId { get; set; }

    [Display("Provenance placement", Description = "When omitted, translation and revision evidence is written to its matching destination.")]
    [StaticDataSource(typeof(ProvenancePlacementDataHandler))]
    public string? Placement { get; set; }
}
