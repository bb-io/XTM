using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class ProjectAcceptedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.ProjectAccepted;
    
    public ProjectAcceptedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}