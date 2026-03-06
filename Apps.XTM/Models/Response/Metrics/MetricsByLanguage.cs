using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Metrics
{
    public class MetricsByLanguage
    {
        [Display("Target language")]
        [JsonProperty("targetLanguage")]
        public string targetLanguage { get; set; }

        [Display("Core metrics")]
        [JsonProperty("coreMetrics")]
        public CoreMetrics coreMetrics { get; set; }
    }
    public class CoreMetrics
    {
        [Display("Ice match words")]
        [JsonProperty("iceMatchWords")]
        public double? iceMatchWords { get; set; }

        [Display("Low fuzzy match words")]
        [JsonProperty("lowFuzzyMatchWords")]
        public double? lowFuzzyMatchWords { get; set; }

        [Display("Medium fuzzy match words")]
        [JsonProperty("mediumFuzzyMatchWords")]
        public double? mediumFuzzyMatchWords { get; set; }

        [Display("High fuzzy match words")]
        [JsonProperty("highFuzzyMatchWords")]
        public double? highFuzzyMatchWords { get; set; }

        [Display("Repeat words")]
        [JsonProperty("repeatsWords")]
        public double? repeatsWords { get; set; }

        [Display("Leveraged words")]
        [JsonProperty("leveragedWords")]
        public double? leveragedWords { get; set; }

        [Display("Low fuzzy repeat words")]
        [JsonProperty("lowFuzzyRepeatsWords")]
        public double? lowFuzzyRepeatsWords { get; set; }

        [Display("Medium fuzzy repeat words")]
        [JsonProperty("mediumFuzzyRepeatsWords")]
        public double? mediumFuzzyRepeatsWords { get; set; }

        [Display("High fuzzy repeat words")]
        [JsonProperty("highFuzzyRepeatsWords")]
        public double? highFuzzyRepeatsWords { get; set; }

        [Display("Non translatable words")]
        [JsonProperty("nonTranslatableWords")]
        public double? nonTranslatableWords { get; set; }

        [Display("Total words")]
        [JsonProperty("totalWords")]
        public double? totalWords { get; set; }

        [Display("Machine translation words")]
        [JsonProperty("machineTranslationWords")]
        public double? machineTranslationWords { get; set; }

        [Display("No match words")]
        [JsonProperty("noMatchWords")]
        public double? noMatchWords { get; set; }
    }
}
