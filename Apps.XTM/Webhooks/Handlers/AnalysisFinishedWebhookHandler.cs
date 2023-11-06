using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class AnalysisFinishedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.AnalysisFinished;

    public AnalysisFinishedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}