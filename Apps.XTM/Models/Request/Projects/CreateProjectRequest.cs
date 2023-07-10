using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects
{
    public class CreateProjectRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        [Display("Customer id")] public long CustomerId { get; set; }
        [Display("Workflow id")] public int WorkflowId { get; set; }
        [Display("Source language")] public string SourceLanguge { get; set; }

        [Display("Target languages")] public IEnumerable<string> TargetLanguges { get; set; }
    }
}
