using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class TermStatusDataHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {
            {"ALL", "All"},
            {"VALID", "Valid"},
            {"NOT_APPROVED", "Not approved"},
            {"REJECTED", "Rejected"},
        };
    }
}
