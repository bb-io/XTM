using System.Net;
using System.Web;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Request.Invoices;
using Apps.XTM.Models.Request.Jobs;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Webhooks.Handlers;
using Apps.XTM.Webhooks.Models.Payload;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using RestSharp;
using Apps.XTM.Invocables;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.XTM.Models.Request;

namespace Apps.XTM.Webhooks;

[WebhookList]
public class WebhookList : XtmInvocable
{
    public WebhookList(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    #region Manual webhooks

    //[Webhook("On analysis finished (manual)", Description = "On project analysis finished (manual)")]
    //public Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinishedManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    //{
    //    var data = HandleFormDataRequest<AnalysisFinishedPayload>(request);
    //    var result = new AnalysisFinishedResponse(data);

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<AnalysisFinishedResponse>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<AnalysisFinishedResponse>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //}

    //[Webhook("On workflow transition (manual)", Description = "On any transition in active workflow steps (manual)")]
    //public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransitionManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
    //    [WebhookParameter] CustomerOptionalRequest customerOptionalRequest)
    //{
    //    var data = HandleFormDataRequest<WorkflowTransitionPayload>(request);
    //    var result = new WorkflowTransitionResponse(data);

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<WorkflowTransitionResponse>();
    //    }
        
    //    if(customerOptionalRequest.CustomerId != null && customerOptionalRequest.CustomerId != result.CustomerId)
    //    {
    //        return GetPreflightResponse<WorkflowTransitionResponse>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //}

    //[Webhook("On job finished (manual)", Description = "On a specific job finished (manual)")]
    //public Task<WebhookResponse<JobFinishedPayload>> OnJobFinishedManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
    //    [WebhookParameter] JobOptionalRequest jobOptionalRequest)
    //{
    //    var result = new JobFinishedPayload
    //    {
    //        ProjectId = request.QueryParameters["xtmProjectId"],
    //        CustomerId = request.QueryParameters["xtmCustomerId"],
    //        JobId = request.QueryParameters["xtmJobId"],
    //        Uuid = request.QueryParameters["xtmUuid"]
    //    };
        
    //    if(jobOptionalRequest.JobId != null && jobOptionalRequest.JobId != result.JobId)
    //    {
    //        return GetPreflightResponse<JobFinishedPayload>();
    //    }

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<JobFinishedPayload>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<JobFinishedPayload>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //}  
    
    //[Webhook("On project created (manual)", Description = "On a new project created (manual)")]
    //public Task<WebhookResponse<ProjectCreatedPayload>> OnProjectCreatedManual(WebhookRequest request)
    //{
    //    return Task.FromResult(new WebhookResponse<ProjectCreatedPayload>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = new()
    //        {
    //            ProjectId = request.QueryParameters["xtmProjectId"],
    //            Uuid = request.QueryParameters["xtmUuid"]
    //        }
    //    });
    //}    
    
    //[Webhook("On project accepted (manual)", Description = "On a specific project accepted (manual)")]
    //public Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAcceptedManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    //{
    //    var result = new ProjectAcceptedPayload
    //    {
    //        ProjectId = request.QueryParameters["xtmProjectId"],
    //        ExternalProjectId = request.QueryParameters["xtmExternalProjectId"],
    //        Uuid = request.QueryParameters["xtmUuid"]
    //    };

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<ProjectAcceptedPayload>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<ProjectAcceptedPayload>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //} 
    
    //[Webhook("On project finished (manual)", Description = "On a specific project finished (manual)")]
    //public Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinishedManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    //{
    //    var result = new ProjectFinishedPayload
    //    {
    //        ProjectId = request.QueryParameters["xtmProjectId"],
    //        CustomerId = request.QueryParameters["xtmCustomerId"],
    //        Uuid = request.QueryParameters["xtmUuid"]
    //    };

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<ProjectFinishedPayload>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<ProjectFinishedPayload>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //}
    
    //[Webhook("On invoice status changed (manual)", Description = "On invoice changed for any project (manual)")]
    //public Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChangedManual(WebhookRequest request,
    //    [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
    //    [WebhookParameter] InvoiceOptionalRequest invoiceStatusChangedRequest)
    //{
    //    var result = new InvoiceStatusChangedPayload
    //    {
    //        ProjectId = request.QueryParameters["xtmProjectId"],
    //        InvoiceStatus = request.QueryParameters["xtmInvoiceStatus"],
    //        Uuid = request.QueryParameters["xtmUuid"]
    //    };
        
    //    if(invoiceStatusChangedRequest.InvoiceId != null && invoiceStatusChangedRequest.InvoiceId != result.Uuid)
    //    {
    //        return GetPreflightResponse<InvoiceStatusChangedPayload>();
    //    }

    //    if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
    //       (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectNameContainsString(result.ProjectId, projectOptionalRequest.ProjectNameContains)))
    //    {
    //        return GetPreflightResponse<InvoiceStatusChangedPayload>();
    //    }
        
    //    return Task.FromResult(new WebhookResponse<InvoiceStatusChangedPayload>
    //    {
    //        HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
    //        Result = result
    //    });
    //}

    #endregion

    #region Bridge webhooks

    [Webhook("On analysis finished", typeof(AnalysisFinishedWebhookHandler),
        Description = "On project analysis finished")]
    public Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        var data = HandleBridgeWebhookRequest<AnalysisFinishedPayload>(request);
        var result = new AnalysisFinishedResponse(data.Payload);

        if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
           (!string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains) && !result.Customer.Name.Contains(projectOptionalRequest.CustomerNameContains))
           || (!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains)))
        {
            return GetPreflightResponse<AnalysisFinishedResponse>();
        }
        
        return Task.FromResult(new WebhookResponse<AnalysisFinishedResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
        });
    }   
    
    [Webhook("On workflow transition", typeof(WorkflowTransitionedWebhookHandler), 
        Description = "On any transition in active workflow steps")]
    public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransition(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] CustomerOptionalRequest customerOptionalRequest,
        [WebhookParameter] WorkflowStepOptionalRequest workflowOptionalRequest)
    {
        var data = HandleBridgeWebhookRequest<WorkflowTransitionPayload>(request);
        var result = new WorkflowTransitionResponse(data.Payload);

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)           
        {
            return GetPreflightResponse<WorkflowTransitionResponse>();
        }

        if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
           && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains))
        {
            return GetPreflightResponse<WorkflowTransitionResponse>();
        }

        if (customerOptionalRequest.CustomerId != null && customerOptionalRequest.CustomerId != result.CustomerId)
        {
            return GetPreflightResponse<WorkflowTransitionResponse>();
        }

        if (!string.IsNullOrEmpty(workflowOptionalRequest.WorkflowStep))
        {
            var anyMatch = data.Payload.Events
                .SelectMany(e => e.Tasks)
                .Any(t => string.Equals(t.Step.WorkflowStepName, workflowOptionalRequest.WorkflowStep, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(t.Step.WorkflowStep, workflowOptionalRequest.WorkflowStep, StringComparison.OrdinalIgnoreCase));

            if (!anyMatch)
                return GetPreflightResponse<WorkflowTransitionResponse>();
        }


        return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
        });
    } 
    
    [Webhook("On job finished", typeof(JobFinishedWebhookHandler), Description = "On a specific job finished")]
    public Task<WebhookResponse<JobFinishedPayload>> OnJobFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] JobOptionalRequest jobOptionalRequest)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        var result = new JobFinishedPayload
        {
            ProjectId = data.Parameters["xtmProjectId"],
            CustomerId = data.Parameters["xtmCustomerId"],
            JobId = data.Parameters["xtmJobId"],
            Uuid = data.Parameters["xtmUuid"]
        };
        
        if(jobOptionalRequest.JobId != null && jobOptionalRequest.JobId != result.JobId)
        {
            return GetPreflightResponse<JobFinishedPayload>();
        }

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
        {
            return GetPreflightResponse<JobFinishedPayload>();
        }

        if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
           && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains))
        {
            return GetPreflightResponse<JobFinishedPayload>();
        }

        return Task.FromResult(new WebhookResponse<JobFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
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
    public Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAccepted(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        var result = new ProjectAcceptedPayload
        {
            ProjectId = data.Parameters["xtmProjectId"],
            ExternalProjectId = data.Parameters["xtmExternalProjectId"],
            Uuid = data.Parameters["xtmUuid"]
        };

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
        {
            return GetPreflightResponse<ProjectAcceptedPayload>();
        }

        if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
           && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains))
        {
            return GetPreflightResponse<ProjectAcceptedPayload>();
        }

        return Task.FromResult(new WebhookResponse<ProjectAcceptedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
        });
    } 
    
    [Webhook("On project finished", typeof(ProjectFinishedWebhookHandler),
        Description = "On a specific project finished")]
    public Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        var result = new ProjectFinishedPayload
        {
            ProjectId = data.Parameters["xtmProjectId"],
            CustomerId = data.Parameters["xtmCustomerId"],
            Uuid = data.Parameters["xtmUuid"]
        };

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
        {
            return GetPreflightResponse<ProjectFinishedPayload>();
        }

        if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
           && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains))
        {
            return GetPreflightResponse<ProjectFinishedPayload>();
        }

        return Task.FromResult(new WebhookResponse<ProjectFinishedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
        });
    }
    
    [Webhook("On invoice status changed", typeof(InvoiceStatusChangedWebhookHandler), 
        Description = "On invoice changed for any project")]
    public Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChanged(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] InvoiceOptionalRequest invoiceStatusChangedRequest)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        var result = new InvoiceStatusChangedPayload
        {
            ProjectId = data.Parameters["xtmProjectId"],
            InvoiceStatus = data.Parameters["xtmInvoiceStatus"],
            Uuid = data.Parameters["xtmUuid"]
        };
        
        if(invoiceStatusChangedRequest.InvoiceId != null && invoiceStatusChangedRequest.InvoiceId != result.Uuid)
        {
            return GetPreflightResponse<InvoiceStatusChangedPayload>();
        }

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
        {
            return GetPreflightResponse<InvoiceStatusChangedPayload>();
        }

        if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
           && !ProjectFilter(result.ProjectId, projectOptionalRequest.ProjectNameContains, projectOptionalRequest.CustomerNameContains))
        {
            return GetPreflightResponse<InvoiceStatusChangedPayload>();
        }

        return Task.FromResult(new WebhookResponse<InvoiceStatusChangedPayload>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
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
    
    private Task<WebhookResponse<T>> GetPreflightResponse<T>()
        where T : class
    {
        return Task.FromResult(new WebhookResponse<T>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = null,
            ReceivedWebhookRequestType = WebhookRequestType.Preflight
        });
    }

    private bool ProjectFilter(string projectId, string projectNameContains, string CustomerNameContains)
    {
        var projectInfo = Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{projectId}", Method.Get, null, Creds).Result;

        if (!String.IsNullOrEmpty(projectNameContains) && !projectInfo.Name.Contains(projectNameContains))
        {
            return false;
        }

        if (!String.IsNullOrEmpty(CustomerNameContains) && !projectInfo.CustomerName.Contains(CustomerNameContains))
        {
            return false;
        }

        return true;


    }

    #endregion  
}