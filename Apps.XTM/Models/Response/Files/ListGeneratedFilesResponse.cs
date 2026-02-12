using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Files;

public record ListGeneratedFilesResponse(GeneratedFileResponse[] Files, [Display("Job IDs")] IEnumerable<string> JobIds);