using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Response.Projects
{
    public class ProjectTemplate
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long CustomerId { get; set; }
    }
}
