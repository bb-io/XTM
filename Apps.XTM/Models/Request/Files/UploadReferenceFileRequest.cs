using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System.ComponentModel;

namespace Apps.XTM.Models.Request.Files;

public class UploadReferenceFileRequest
{
    [Display("Reference file")]
    public FileReference File { get; set; } = default!;

    [Display("Override file name", Description = "Overrides the original filename.")]
    public string? Name { get; set; }
}

