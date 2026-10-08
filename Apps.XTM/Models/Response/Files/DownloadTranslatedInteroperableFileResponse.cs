using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.Files;

public class DownloadTranslatedInteroperableFileResponse
{
    public FileReference File { get; set; } = new();

    [Display("Translation file", Description = "XTM offline XLIFF with references to the full interoperable file's segments.")]
    public FileReference TranslationFile { get; set; } = new();
}
