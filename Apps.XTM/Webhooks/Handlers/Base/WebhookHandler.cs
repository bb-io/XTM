using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.Sdk.Utils.Webhooks.Bridge;
using Blackbird.Applications.Sdk.Utils.Webhooks.Bridge.Models.Request;

namespace Apps.XTM.Webhooks.Handlers.Base;

public abstract class WebhookHandler : BaseInvocable, IWebhookEventHandler
{
    protected abstract string Event { get; }
    
    protected WebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task SubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> credentials, 
        Dictionary<string, string> values)
    {
        var (input, bridgeCredentials) = GetBridgeServiceInputs(values, credentials);
        await BridgeService.Subscribe(input, bridgeCredentials);
    }

    public async Task UnsubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> credentials,
        Dictionary<string, string> values)
    {
        var (input, bridgeCredentials) = GetBridgeServiceInputs(values, credentials);
        await BridgeService.Unsubscribe(input, bridgeCredentials);
    }
    
    private (BridgeRequest webhookData, BridgeCredentials bridgeCreds) GetBridgeServiceInputs(
        Dictionary<string, string> values, IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var webhookData = new BridgeRequest
        {
            Event = Event,
            Id = credentials.GetInstanceUrlHash(),
            Url = values["payloadUrl"]
        };

        var bridgeCredentials = new BridgeCredentials
        {
            ServiceUrl = $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}{ApplicationConstants.XtmBridgePath}",
            Token = ApplicationConstants.BlackbirdToken
        };

        return (webhookData, bridgeCredentials);
    }
}