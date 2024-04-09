using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class TermColumnsDataHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {
            {"ABBREVIATION", "Abbreviation"},
            {"CONTEXT", "Context"},
            {"DOMAIN", "Domain"},
            {"LAST_MOD_DATE", "Last mod date"},
            {"REFERENCE", "Reference"},
            {"REMARKS", "Remarks"},
            {"DEFINITION", "Definition"},
            {"LAST_MOD_BY", "Last mod by"},
            {"STATUS", "Status"},
        };
    }
}
