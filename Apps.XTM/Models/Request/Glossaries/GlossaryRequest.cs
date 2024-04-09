using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Request.Glossaries
{
    public class GlossaryRequest
    {
        [Display("Customer ID")]
        [DataSource(typeof(CustomerDataHandler))]
        public string CustomerId { get; set; }

        [Display("Main language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string? MainLanguage { get; set; }

        [Display("Translation languages")]
        [DataSource(typeof(LanguageDataHandler))]
        public List<string>? Languages { get; set; }

        [Display("Status")]
        [DataSource(typeof(TermStatusDataHandler))]
        public string? Status { get; set; }

        [Display("Columns to export")]
        [DataSource(typeof(TermColumnsDataHandler))]
        public List<string>? Columns { get; set; }

        [Display("Domain")]
        public string? Domain { get; set; }

        [Display("All languages")]
        public bool? AllLanguages { get; set; }
    }
}
