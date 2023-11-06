using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class ProjectCreatedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.ProjectCreated;
    
    public ProjectCreatedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}