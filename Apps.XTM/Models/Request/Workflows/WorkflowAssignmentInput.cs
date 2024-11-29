using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Workflows
{
    public class WorkflowAssignmentInput
    {
        [Display("Project ID")]
        [DataSource(typeof(ProjectDataHandler))]
        public string ProjectId { get; set; }

        [Display("User information")]
        public WorkFlowUser User { get; set; }

        [Display("Languages")]
        public List<string>? Languages { get; set; }

        [Display("Step names")]
        public List<string>? StepName { get; set; }

        [Display("Job IDs")]
        public List<string>? JobIds { get; set; }

        [Display("Bundle IDs")]
        public List<string>? BundleIds { get; set; }

    }

    public class WorkFlowUser
    {
        [Display("User ID")]
        public string Id { get; set; }

        [Display("User type")]
        public string Type {  get; set; }
    }

}
