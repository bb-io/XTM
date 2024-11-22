using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Metrics
{
    public class MetricByLanguagesResponse
    {
        [Display("Metrics per language")]
        public IEnumerable<MetricsByLanguage> Metrics { get; set; }
    }
}
