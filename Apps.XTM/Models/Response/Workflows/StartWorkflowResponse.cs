using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows
{
    public class StartWorkflowResponse
    {
        [Display("Project ID")]
        public long ProjectId { get; set; }

        [Display("Status of workflow")]
        public bool Status { get; set; }

        [Display("Jobs")]
        public IEnumerable<JobStatus> Jobs { get; set; }

        [Display("Error message")]
        public string? ErrorType { get; set; }
    }

    public class JobStatus
    {
        [Display("Job ID")]
        public long JobId { get; set; }

        [Display("Job status")]
        public bool Status { get; set; }
    }
}
