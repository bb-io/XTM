using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class DownloadMetricsRequest
{
    [Display("Metrics files type")]
    [DataSource(typeof(MetricsFilesTypeDataHandler))]
    public string MetricsFilesType { get; set; }
}