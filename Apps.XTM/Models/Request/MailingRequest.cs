using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request
{
    public class MailingRequest
    {
        [Display("Email notify")]
        [DataSource(typeof(MailingStatusHandler))]
        public string? Mailing {  get; set; }
    }
}
