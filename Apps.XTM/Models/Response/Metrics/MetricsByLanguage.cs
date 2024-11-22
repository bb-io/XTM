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
        public int iceMatchWords { get; set; }

        [Display("Low fuzzy match words")]
        [JsonProperty("lowFuzzyMatchWords")]
        public int lowFuzzyMatchWords { get; set; }

        [Display("Medium fuzzy match words")]
        [JsonProperty("mediumFuzzyMatchWords")]
        public int mediumFuzzyMatchWords { get; set; }

        [Display("High fuzzy match words")]
        [JsonProperty("highFuzzyMatchWords")]
        public int highFuzzyMatchWords { get; set; }

        [Display("Repeat words")]
        [JsonProperty("repeatsWords")]
        public int repeatsWords { get; set; }

        [Display("Leveraged words")]
        [JsonProperty("leveragedWords")]
        public int leveragedWords { get; set; }

        [Display("Low fuzzy repeat words")]
        [JsonProperty("lowFuzzyRepeatsWords")]
        public int lowFuzzyRepeatsWords { get; set; }

        [Display("Medium fuzzy repeat words")]
        [JsonProperty("mediumFuzzyRepeatsWords")]
        public int mediumFuzzyRepeatsWords { get; set; }

        [Display("High fuzzy repeat words")]
        [JsonProperty("highFuzzyRepeatsWords")]
        public int highFuzzyRepeatsWords { get; set; }

        [Display("Non translatable words")]
        [JsonProperty("nonTranslatableWords")]
        public int nonTranslatableWords { get; set; }

        [Display("Total words")]
        [JsonProperty("totalWords")]
        public int totalWords { get; set; }

        [Display("Machine translation words")]
        [JsonProperty("machineTranslationWords")]
        public int machineTranslationWords { get; set; }

        [Display("No match words")]
        [JsonProperty("noMatchWords")]
        public int noMatchWords { get; set; }
    }
}
