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
            Result = new(data)
            {
                CustomerId = request.QueryParameters["xtmCustomerId"]
            },
        });
    }    
    
    [Webhook("On job finished", Description = "On a specific job finished")]
    public Task<WebhookResponse<JobFinishedPayload>> OnJobFinished(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<JobFinishedPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                CustomerId = request.QueryParameters["xtmCustomerId"],
                JobId = request.QueryParameters["xtmJobId"],
                Uuid = request.QueryParameters["xtmUuid"],
            },
        });
    }  
    
    [Webhook("On project created", Description = "On a new project created")]
    public Task<WebhookResponse<ProjectCreatedPayload>> OnProjectCreated(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectCreatedPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                Uuid = request.QueryParameters["xtmUuid"],
            },
        });
    }    
    
    [Webhook("On project accepted", Description = "On a specific project accepted")]
    public Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAccepted(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectAcceptedPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                ExternalProjectId = request.QueryParameters["xtmExternalProjectId"],
                Uuid = request.QueryParameters["xtmUuid"],
            },
        });
    } 
    
    [Webhook("On project finished", Description = "On a specific project finished")]
    public Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinished(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectFinishedPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                CustomerId = request.QueryParameters["xtmCustomerId"],
                Uuid = request.QueryParameters["xtmUuid"],
            },
        });
    }
    
    [Webhook("On invoice status changed", Description = "On invoice changed for any project")]
    public Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChanged(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<InvoiceStatusChangedPayload>()
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                InvoiceStatus = request.QueryParameters["xtmInvoiceStatus"],
                Uuid = request.QueryParameters["xtmUuid"],
            },
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