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
        [Display("Glossary ID")]
        //[DataSource(typeof(GlossariesDataHandler))]
        public string GlossaryId { get; set; }
    }
}
