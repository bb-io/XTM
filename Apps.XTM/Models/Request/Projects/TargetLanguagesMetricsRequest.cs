using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Request.Projects
{
    public class TargetLanguagesMetricsRequest
    {
        [Display("Target languages")]
        [DataSource(typeof(LanguageDataHandler))]
        public IEnumerable<string>? TargetLanguages { get; set; }
    }
}
