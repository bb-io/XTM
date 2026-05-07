using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Projects;

public record ExportProjectAnalysisResponse(FileReference ExportedAnalysis)
{
    [Display("Analysis file")] 
    public FileReference ExportedAnalysis { get; set; } = ExportedAnalysis;
}