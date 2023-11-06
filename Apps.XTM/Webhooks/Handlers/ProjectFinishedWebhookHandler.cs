using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class ProjectFinishedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.ProjectFinished;
    
    public ProjectFinishedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}