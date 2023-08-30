using System.Net;
using System.Web;
using Apps.XTM.Webhooks.Models.Payload;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;

namespace Apps.XTM.Webhooks;

[WebhookList]
public class WebhookList
{
    #region Webhooks

    [Webhook("On analysis finished", Description = "On project analysis finished")]
    public Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinished(WebhookRequest request)
    {
        var data = HandleFormDataRequest<AnalysisFinishedPayload>(request);
        
        return Task.FromResult(new WebhookResponse<AnalysisFinishedResponse>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data),
        });
    }    
    
    [Webhook("On workflow transition", Description = "On any transition in active workflow steps")]
    public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransition(WebhookRequest request)
    {
        var data = HandleFormDataRequest<WorkflowTransitionPayload>(request);
        
        return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data),
        });
    }    
    
    [Webhook("On job finished", Description = "On a specific job finished")]
    public Task<WebhookResponse<EmptyPayload>> OnJobFinished(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<EmptyPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(),
        });
    }  
    
    [Webhook("On project created", Description = "On a new project created")]
    public Task<WebhookResponse<EmptyPayload>> OnProjectCreated(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<EmptyPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(),
        });
    }    
    
    [Webhook("On project accepted", Description = "On a specific project accepted")]
    public Task<WebhookResponse<EmptyPayload>> OnProjectAccepted(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<EmptyPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(),
        });
    } 
    
    [Webhook("On project finished", Description = "On a specific project finished")]
    public Task<WebhookResponse<EmptyPayload>> OnProjectFinished(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<EmptyPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(),
        });
    }
    
    [Webhook("On invoice status changed", Description = "On invoice changed for any project")]
    public Task<WebhookResponse<EmptyPayload>> OnInvoiceStatusChanged(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<EmptyPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(),
        });
    }

    #endregion

    #region Utils

    public T HandleFormDataRequest<T>(WebhookRequest request)
    {
        var formData = HttpUtility.ParseQueryString(request.Body.ToString());
        var jsonData = formData["additionalData"];

        return JsonConvert.DeserializeObject<T>(jsonData);
    } 

    #endregion  
}