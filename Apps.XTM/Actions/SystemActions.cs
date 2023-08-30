using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.System;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class SystemActions : XtmInvocable
{
    public SystemActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("Get system", Description = "Get system details")]
    public Task<SystemResponse> GetSystem()
    {
        return Client.ExecuteXtmWithJson<SystemResponse>(ApiEndpoints.System,
            Method.Get,
            null,
            Creds);
    }
}