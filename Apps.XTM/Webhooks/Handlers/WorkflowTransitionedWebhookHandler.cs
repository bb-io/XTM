using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class WorkflowTransitionedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.WorkflowTransition;
    
    public WorkflowTransitionedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}