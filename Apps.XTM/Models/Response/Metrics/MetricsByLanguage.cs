using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Metrics
{
    public class MetricsByLanguage
    {
        [Display("Target language")]
        public string targetLanguage { get; set; }

        [Display("Metrics")]
        public CoreMetrics coreMetrics { get; set; }
    }
    public class CoreMetrics
    {
        [Display("Ice match words")]
        public int iceMatchWords { get; set; }

        [Display("Low fuzzy match words")]
        public int lowFuzzyMatchWords { get; set; }

        [Display("Medium fuzzy match words")]
        public int mediumFuzzyMatchWords { get; set; }

        [Display("High fuzzy match words")]
        public int highFuzzyMatchWords { get; set; }

        [Display("Repeat words")]
        public int repeatsWords { get; set; }

        [Display("Leveraged words")]
        public int leveragedWords { get; set; }

        [Display("Low fuzzy repeat words")]
        public int lowFuzzyRepeatsWords { get; set; }

        [Display("Medium fuzzy repeat words")]
        public int mediumFuzzyRepeatsWords { get; set; }

        [Display("High fuzzy repeat words")]
        public int highFuzzyRepeatsWords { get; set; }

        [Display("Non translatable words")]
        public int nonTranslatableWords { get; set; }

        [Display("Total words")]
        public int totalWords { get; set; }

        [Display("Machine translation words")]
        public int machineTranslationWords { get; set; }

        [Display("No match words")]
        public int noMatchWords { get; set; }
    }
}
