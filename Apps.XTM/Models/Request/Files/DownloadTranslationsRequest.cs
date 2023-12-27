using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Files;

public class DownloadTranslationsRequest
{
    [Display("Job IDs")]
    public IEnumerable<string>? JobIds { get; set; }

    [Display("Target languages")]
    [DataSource(typeof(ProjectTargetLanguageDataSourceHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
}