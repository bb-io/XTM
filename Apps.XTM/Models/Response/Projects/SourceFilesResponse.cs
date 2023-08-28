using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.XTM.Models.Response.Projects;

public record SourceFilesResponse(List<File> Files);