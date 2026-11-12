using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Files;

public class UploadSourceFileMatchRequest
{
    [Display("Replace existing file with same name", Description = "If enabled, a source file with the same name already uploaded to the project is replaced. Otherwise the file is added and renamed if a file with the same name exists.")]
    public bool? ReplaceExistingFile { get; set; }

    public string? GetMatchType() => ReplaceExistingFile switch
    {
        true => "MATCH_NAMES",
        false => "NO_MATCH",
        null => null
    };
}
