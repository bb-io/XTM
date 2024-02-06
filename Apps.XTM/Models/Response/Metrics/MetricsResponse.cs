using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Metrics
{
    public class MetricsResponse
    {
        [Display("Job ID")]
        public string JobId { get; set; }
        [Display("Step name")]
        public string StepName { get; set; }
        public Bundle Bundle { get; set; }
    }
}
