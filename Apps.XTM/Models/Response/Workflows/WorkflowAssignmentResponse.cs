using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows
{
    public class WorkflowAssignmentResponse
    {
        [Display("Success")]
        public bool Success { get; set; }

        [Display("Errors")]
        public List<string>? Errors { get; set; } 
    }
}
