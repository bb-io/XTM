using System.Net;
using System.Web;
using Apps.XTM.Webhooks.Handlers;
using Apps.XTM.Webhooks.Models.Payload;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;

namespace Apps.XTM.Webhooks;

[WebhookList]
public class WebhookList
{
    #region Manual webhooks

    [Webhook("On analysis finished (manual)", Description = "On project analysis finished (manual)")]
    public Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinishedManual(WebhookRequest request)
    {
        var data = HandleFormDataRequest<AnalysisFinishedPayload>(request);
        
        return Task.FromResult(new WebhookResponse<AnalysisFinishedResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data)
        });
    }

    [Webhook("On workflow transition (manual)", Description = "On any transition in active workflow steps (manual)")]
    public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransitionManual(WebhookRequest request)
    {
        var data = HandleFormDataRequest<WorkflowTransitionPayload>(request);
        
        return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data)
            {
                CustomerId = request.QueryParameters["xtmCustomerId"]
            }
        });
    }

    [Webhook("On job finished (manual)", Description = "On a specific job finished (manual)")]
    public Task<WebhookResponse<JobFinishedPayload>> OnJobFinishedManual(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<JobFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                CustomerId = request.QueryParameters["xtmCustomerId"],
                JobId = request.QueryParameters["xtmJobId"],
                Uuid = request.QueryParameters["xtmUuid"]
            }
        });
    }  
    
    [Webhook("On project created (manual)", Description = "On a new project created (manual)")]
    public Task<WebhookResponse<ProjectCreatedPayload>> OnProjectCreatedManual(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectCreatedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                Uuid = request.QueryParameters["xtmUuid"]
            }
        });
    }    
    
    [Webhook("On project accepted (manual)", Description = "On a specific project accepted (manual)")]
    public Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAcceptedManual(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectAcceptedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                ExternalProjectId = request.QueryParameters["xtmExternalProjectId"],
                Uuid = request.QueryParameters["xtmUuid"]
            }
        });
    } 
    
    [Webhook("On project finished (manual)", Description = "On a specific project finished (manual)")]
    public Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinishedManual(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<ProjectFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                CustomerId = request.QueryParameters["xtmCustomerId"],
                Uuid = request.QueryParameters["xtmUuid"]
            }
        });
    }
    
    [Webhook("On invoice status changed (manual)", Description = "On invoice changed for any project (manual)")]
    public Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChangedManual(WebhookRequest request)
    {
        return Task.FromResult(new WebhookResponse<InvoiceStatusChangedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = request.QueryParameters["xtmProjectId"],
                InvoiceStatus = request.QueryParameters["xtmInvoiceStatus"],
                Uuid = request.QueryParameters["xtmUuid"]
            }
        });
    }

    #endregion

    #region Bridge webhooks

    [Webhook("On analysis finished", typeof(AnalysisFinishedWebhookHandler),
        Description = "On project analysis finished")]
    public Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinished(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<AnalysisFinishedPayload>(request);
        
        return Task.FromResult(new WebhookResponse<AnalysisFinishedResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data.Payload)
        });
    }   
    
    [Webhook("On workflow transition", typeof(WorkflowTransitionedWebhookHandler), 
        Description = "On any transition in active workflow steps")]
    public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransition(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<WorkflowTransitionPayload>(request);
        
        return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new(data.Payload)
            {
                CustomerId = data.Parameters["xtmCustomerId"]
            }
        });
    } 
    
    [Webhook("On job finished", typeof(JobFinishedWebhookHandler), Description = "On a specific job finished")]
    public Task<WebhookResponse<JobFinishedPayload>> OnJobFinished(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        
        return Task.FromResult(new WebhookResponse<JobFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = data.Parameters["xtmProjectId"],
                CustomerId = data.Parameters["xtmCustomerId"],
                JobId = data.Parameters["xtmJobId"],
                Uuid = data.Parameters["xtmUuid"]
            },
        });
    }
    
    [Webhook("On project created", typeof(ProjectCreatedWebhookHandler), Description = "On a new project created")]
    public Task<WebhookResponse<ProjectCreatedPayload>> OnProjectCreated(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        
        return Task.FromResult(new WebhookResponse<ProjectCreatedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = data.Parameters["xtmProjectId"],
                Uuid = data.Parameters["xtmUuid"]
            }
        });
    }    
    
    [Webhook("On project accepted", typeof(ProjectAcceptedWebhookHandler),
        Description = "On a specific project accepted")]
    public Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAccepted(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        
        return Task.FromResult(new WebhookResponse<ProjectAcceptedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = data.Parameters["xtmProjectId"],
                ExternalProjectId = data.Parameters["xtmExternalProjectId"],
                Uuid = data.Parameters["xtmUuid"]
            }
        });
    } 
    
    [Webhook("On project finished", typeof(ProjectFinishedWebhookHandler),
        Description = "On a specific project finished")]
    public Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinished(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        
        return Task.FromResult(new WebhookResponse<ProjectFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = data.Parameters["xtmProjectId"],
                CustomerId = data.Parameters["xtmCustomerId"],
                Uuid = data.Parameters["xtmUuid"]
            }
        });
    }
    
    [Webhook("On invoice status changed", typeof(InvoiceStatusChangedWebhookHandler), 
        Description = "On invoice changed for any project")]
    public Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChanged(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        
        return Task.FromResult(new WebhookResponse<InvoiceStatusChangedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new()
            {
                ProjectId = data.Parameters["xtmProjectId"],
                InvoiceStatus = data.Parameters["xtmInvoiceStatus"],
                Uuid = data.Parameters["xtmUuid"]
            }
        });
    }

    #endregion
    
    #region Utils

    private T HandleFormDataRequest<T>(WebhookRequest request)
    {
        var formData = HttpUtility.ParseQueryString(request.Body.ToString());
        var jsonData = formData["additionalData"];

        return JsonConvert.DeserializeObject<T>(jsonData);
    }

    private BridgeWebhookPayload<T> HandleBridgeWebhookRequest<T>(WebhookRequest request) 
        => JsonConvert.DeserializeObject<BridgeWebhookPayload<T>>(request.Body.ToString());

    #endregion  
}