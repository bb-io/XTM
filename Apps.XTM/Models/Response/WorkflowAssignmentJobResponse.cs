using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Response
{
    public class WorkflowAssignmentJobResponse
    {
        [JsonProperty("jobs")]
        public List<WorkflowJob> Jobs { get; set; }
    }
    public class WorkflowJob
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("steps")]
        public List<WorkflowStep> Steps { get; set; }
    }

    public class WorkflowStep
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("bundles")]
        public List<WorkflowBundle> Bundles { get; set; }
    }

    public class WorkflowBundle
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("userId")]
        public string? UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }
    }
}
