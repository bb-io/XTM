

using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response
{
    public class LqaResponse
    {
        [Display("LQA report ID")]
        public string id { get; set; }

        [Display("Severity multiplier neutral")]
        public string severityMultiplierNeutral { get; set; }

        [Display("Severity multiplier minor")]
        public string severityMultiplierMinor { get; set; }

        [Display("Severity multiplier major")]
        public string severityMultiplierMajor { get; set; }

        [Display("Severity multiplier critical")]
        public string severityMultiplierCritical { get; set; }

        [Display("Completion date")]
        public string completeDate { get; set; }

        [Display("Evaluee name")]
        public string evaluee { get; set; }

        [Display("Evaluator name")]
        public string evaluator { get; set; }

        [Display("Customer name")]
        public string customer { get; set; }

        [Display("Project ID")]
        public string projectId { get; set; }

        [Display("Project name")]
        public string projectName { get; set; }

        [Display("Subject matter")]
        public string SubjectMatter { get; set; }

        [Display("Project wordcount")]
        public int projectWordcount { get; set; }

        [Display("Project total")]
        public Total projectTotal { get; set; }

        [Display("Project errors")]
        public List<Error> projectErrors { get; set; }

        [Display("Language code")]
        public string languageCode { get; set; }

        [Display("Language wordcount")]
        public int languageWordcount { get; set; }

        [Display("Language total")]
        public Total languageTotal { get; set; }

        [Display("Language errors")]
        public List<Error> languageErrors { get; set; }

        [Display("Files")]
        public List<File>? files { get; set; }
    }
   
}
