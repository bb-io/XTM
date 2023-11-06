using Apps.XTM.Constants;
using Apps.XTM.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Webhooks.Handlers;

public class InvoiceStatusChangedWebhookHandler : WebhookHandler
{
    protected override string Event => EventNames.InvoiceStatusChanged;
    
    public InvoiceStatusChangedWebhookHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}