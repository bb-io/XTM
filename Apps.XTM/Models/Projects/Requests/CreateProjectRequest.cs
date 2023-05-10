using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Projects.Requests
{
    public class CreateProjectRequest
    {
        public long CustomerId { get; set; }

        public string ProjectName { get; set; }

        public int WorkflowId { get; set; } // 0 - translate

        public int SourceLanguge { get; set; } // en_US - 79

        public int TargetLanguge { get; set; } // nl_NL - 78
    }
}
