using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class UserTypeHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {
            { "INTERNAL_USER", "Internal User" },
            { "LSP", "Language Service Provider (LSP)" },
            { "USER_GROUP", "User Group" }
        };
    }
}
