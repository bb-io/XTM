using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class JobFinishedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.JobFinished;
    
    public JobFinishedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}