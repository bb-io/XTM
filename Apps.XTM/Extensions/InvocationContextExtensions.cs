using Apps.XTM.Constants;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.String;

namespace Apps.XTM.Extensions;

public static class InvocationContextExtensions
{
    public static string GetBridgeUrl(this InvocationContext invocationContext, string eventType)
    {
        return 
            $"{invocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}{ApplicationConstants.XtmBridgePath}"
                .SetQueryParameter("id", invocationContext.AuthenticationCredentialsProviders.GetInstanceUrlHash())
                .SetQueryParameter("eventType", eventType);
    }
}
