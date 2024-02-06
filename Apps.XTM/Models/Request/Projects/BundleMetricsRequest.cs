using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects
{
    public class BundleMetricsRequest
    {
        [Display("Job ID")]
        public string? JobId { get; set; }
    }
}
