using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.DataSourceHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects
{
    public class CreateProjectFromTemplateRequest : CustomerRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [Display("Template")]
        [DataSource(typeof(ProjectTemplateDataHandler))]
        public string TemplateId { get; set; }

    }
}
