

using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response
{
    public class LqaResponse
    {
        [Display("LQA report ID")]
        public string id { get; set; }

        [Display("Severity multipliers")]
        public SeverityMultipliers severityMultipliers { get; set; }

        [Display("Completion date")]
        public string completeDate { get; set; }

        [Display("Evaluee")]
        public Evaluee evaluee { get; set; }

        [Display("Evaluator")]
        public Evaluator evaluator { get; set; }

        [Display("Customer")]
        public Customer customer { get; set; }

        [Display("Project")]
        public Project project { get; set; }

        [Display("Language")]
        public Language language { get; set; }

        [Display("Files")]
        public List<File>? files { get; set; }
    }
    public class Customer
    {
        [Display("Customer ID")]
        public int id { get; set; }

        [Display("Customer name")]
        public string name { get; set; }
    }

    public class Error
    {
        [Display("Error ID")]
        public string id { get; set; }

        [Display("Parent ID")]
        public string parentId { get; set; }

        [Display("Parent Path")]
        public string parentPath { get; set; }

        [Display("Key")]
        public string key { get; set; }

        [Display("Weight")]
        public string weight { get; set; }

        [Display("Issue count")]
        public IssueCounts issueCounts { get; set; }

        [Display("Penalty")]
        public Penalty penalty { get; set; }

        [Display("Target subscore")]
        public string targetSubscore { get; set; }
    }

    public class Evaluator
    {
        [Display("Evaluator ID")]
        public int userId { get; set; }

        [Display("Evaluator Name")]
        public string userName { get; set; }
    }

    public class Evaluee
    {
        [Display("Evaluaee ID")]
        public int userId { get; set; }

        [Display("Evaluaee name")]
        public string userName { get; set; }
    }

    public class File
    {
        public _File file { get; set; }

        [Display("Job ID")]
        public int jobId { get; set; }

        [Display("Word count")]
        public int wordCount { get; set; }
        public Total total { get; set; }
        public List<Error> errors { get; set; }
    }

    public class _File
    {
        [Display("File ID")]
        public int fileId { get; set; }

        [Display("File name")]
        public string fileName { get; set; }
    }

    public class IssueCounts
    {
        public int Neutral { get; set; }
        public int Minor { get; set; }
        public int Major { get; set; }
        public int Critical { get; set; }
    }

    public class Language
    {
        [Display("Language code")]
        public string code { get; set; }

        [Display("Word count")]
        public int wordCount { get; set; }
        public Total Total { get; set; }
        public List<Error> Errors { get; set; }
    }

    public class Penalty
    {
        public string Raw { get; set; }

        [Display("Adjusted")]
        public string adj { get; set; }
    }

    public class Project
    {
        [Display("Project ID")]
        public int id { get; set; }

        [Display("Project name")]
        public string name { get; set; }

        [Display("Subject Matter")]
        public SubjectMatter subjectMatter { get; set; }

        [Display("Word count")]
        public int wordCount { get; set; }
        public Total Total { get; set; }
        public List<Error> Errors { get; set; }
    }

    public class SeverityMultipliers
    {
        public string Neutral { get; set; }
        public string Minor { get; set; }
        public string Major { get; set; }
        public string Critical { get; set; }
    }

    public class SubjectMatter
    {
        [Display("Subject matter ID")]
        public int id { get; set; }
        public string Name { get; set; }
    }

    public class Total
    {
        public string Weight { get; set; }

        [Display("Issue count")]
        public IssueCounts issueCounts { get; set; }
        public Penalty Penalty { get; set; }

        [Display("Target subscore")]
        public string targetSubscore { get; set; }
    }
}
