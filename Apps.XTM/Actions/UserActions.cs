using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.User;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class UserActions : XtmInvocable
{
    public UserActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("List users", Description = "List all users")]
    public async Task<AllUsersResponse> ListUsers()
    {
        var response = await Client.ExecuteXtmWithJson<List<UserResponse>>($"{ApiEndpoints.Users}",
            Method.Get,
            null,
            Creds);

        return new(response);
    }
}