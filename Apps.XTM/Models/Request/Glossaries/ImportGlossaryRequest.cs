using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Request.Glossaries
{
    public class ImportGlossaryRequest
    {
        [Display("Glossary", Description = "Glossary file exported from other Blackbird apps")]
        public FileReference File { get; set; }

        [Display("Customer ID")]
        [DataSource(typeof(CustomerDataHandler))]
        public string CustomerId { get; set; }

        [Display("Purge terms")]
        public bool? PurgeTerms { get; set; }

        [Display("Add to existing terms")]
        public bool? AddToExisting { get; set; }
    }
}
