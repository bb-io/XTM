using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.User;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers
{
    public class UserDataHandler : XtmInvocable, IAsyncDataSourceHandler
    {
        public UserDataHandler(InvocationContext invocationContext): base(invocationContext)
        {

        }

        public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
        {
            var response = await Client.ExecuteXtmWithJson<List<UserResponse>>(ApiEndpoints.Users, RestSharp.Method.Get, null, Creds);

            return response.Where(x=> context.SearchString == null || x.Username.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToDictionary(x=> x.Id, x=>x.Username);
        }
    }
}
