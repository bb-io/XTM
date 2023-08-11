using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects
{
    public class CreateProjectRequest : CustomerRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        
        [Display("Workflow id")] 
        [DataSource(typeof(WorkflowDataHandler))]
        public string WorkflowId { get; set; }
        
        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguge { get; set; }

        [Display("Target languages")] public IEnumerable<string> TargetLanguges { get; set; }
    }
}
