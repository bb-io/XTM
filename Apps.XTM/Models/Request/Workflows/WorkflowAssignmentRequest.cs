using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Workflows
{
    public class WorkflowAssignmentRequest
    {
        [Display("User information")]
        [DataSource(typeof(UserDataHandler))] 
        public string UserId { get; set; }

        [Display("Type of user on a project")]
        [DataSource(typeof(UserTypeHandler))]
        public string? UserType {  get; set; }

        [Display("Languages")]
        [DataSource(typeof(LanguageDataHandler))]
        public IEnumerable<string>? Languages { get; set; }

        [Display("Step names")]
        public IEnumerable<string>? StepName { get; set; }

        [Display("Job IDs")]
        public IEnumerable<string>? JobIds { get; set; }

        [Display("Bundle IDs")]
        public IEnumerable<string>? BundleIds { get; set; }
    }

   
}
