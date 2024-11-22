using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Metrics
{
    public class MetricByLanguagesResponse
    {
        [Display("Metrics per language")]
        public List<MetricsByLanguage> Metrics { get; set; }
    }
}
