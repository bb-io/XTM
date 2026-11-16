using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Request.Invoices;
using Apps.XTM.Models.Request.Jobs;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Webhooks.Handlers;
using Apps.XTM.Webhooks.Models.Payload;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace Apps.XTM.Webhooks;

[WebhookList]
public class WebhookList(InvocationContext invocationContext) : XtmInvocable(invocationContext)
{
    #region Manual webhooks

    [Webhook("On workflow transition (manual)", Description = "Triggers when jobs move into active workflow steps or finish a selected step")]
    public Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransitionManual(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] CustomerOptionalRequest customerOptionalRequest,
        [WebhookParameter] WorkflowStepOptionalRequest workflowOptionalRequest)
    {
        try
        {
            var hasQueryParams = request.QueryParameters != null! && request.QueryParameters.Any();
            if (string.IsNullOrEmpty(request.Body?.ToString()) && !hasQueryParams)
            {
                return GetPreflightResponse<WorkflowTransitionResponse>();
            }

            var flightOnEventTypes = workflowOptionalRequest.TriggerWhenStepFinished == true
                ? new List<string> { "FINISHED" }
                : new List<string> { "OPENED", "ACTIVE" };

            BridgeWebhookPayload<WorkflowTransitionPayload> data = new();

            if (request.Body?.ToString()?.StartsWith("additionalData=") == true)
            {
                var parsedBody = HttpUtility.ParseQueryString(request.Body?.ToString() ?? string.Empty);
                var additionalDataJson = parsedBody["additionalData"];

                if (!string.IsNullOrEmpty(additionalDataJson))
                {
                    data = new BridgeWebhookPayload<WorkflowTransitionPayload>
                    {
                        Payload = JsonConvert.DeserializeObject<WorkflowTransitionPayload>(additionalDataJson),
                        Parameters = request.QueryParameters ?? [],
                    };
                }
            }
            else if (request.Body?.ToString()?.Contains("additionalData") == true)
            {
                var outerJson = Newtonsoft.Json.Linq.JObject.Parse(request.Body?.ToString() ?? string.Empty);
                var additionalDataString = outerJson["additionalData"]?.ToString();
                data = new BridgeWebhookPayload<WorkflowTransitionPayload>
                {
                    Parameters = request.QueryParameters ?? [],
                    Payload = !string.IsNullOrEmpty(additionalDataString)
                        ? JsonConvert.DeserializeObject<WorkflowTransitionPayload>(additionalDataString)
                        : null,
                };
            }
            else
            {
                data = HandleBridgeWebhookRequest<WorkflowTransitionPayload>(request);
            }

            if (data.Payload == null)
                return GetPreflightResponse<WorkflowTransitionResponse>();

            var result = new WorkflowTransitionResponse(data.Payload);

            if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
                return GetPreflightResponse<WorkflowTransitionResponse>();

            if ((!string.IsNullOrEmpty(projectOptionalRequest.ProjectNameContains) || !string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains))
               && !ProjectFilter(result.ProjectId, projectOptionalRequest?.ProjectNameContains ?? "", projectOptionalRequest?.CustomerNameContains ?? ""))
            {
                return GetPreflightResponse<WorkflowTransitionResponse>();
            }

            if (customerOptionalRequest.CustomerId != null && customerOptionalRequest.CustomerId != result.CustomerId)
                return GetPreflightResponse<WorkflowTransitionResponse>();

            if (!string.IsNullOrEmpty(workflowOptionalRequest.WorkflowStep))
            {
                var anyMatch = data.Payload.Events
                    .Where(e => flightOnEventTypes.Contains(e.Type))
                    .SelectMany(e => e.Tasks)
                    .Any(t =>
                    {
                        return IsMatchingWorkflowStep(t.Step, workflowOptionalRequest.WorkflowStep);
                    });

                if (!anyMatch)
                    return GetPreflightResponse<WorkflowTransitionResponse>();
            }

            if (data.Parameters.TryGetValue("xtmCustomerId", out string? customerId))
                result.CustomerId = customerId;

            return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
            {
                HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                Result = result
            });
        }
        catch (Exception ex)
        {
            var queryParams = String.Join(", ", request.QueryParameters.Select(kv => $"{kv.Key}: {kv.Value}"));
            InvocationContext.Logger?.LogError($"[XTM_OnWorkflowTransitionManual_Callback]  URL params: {queryParams}; Body: {request.Body?.ToString()}; Stack trace: {ex.StackTrace?.ToString()}", []);

            var response = new HttpResponseMessage(statusCode: HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"Error processing webhook: {ex.Message}. Stack trace: {ex.StackTrace?.ToString()}. Bode received: {request.Body}")
            };

            return Task.FromResult(new WebhookResponse<WorkflowTransitionResponse>
            {
                HttpResponseMessage = response,
                Result = null,
                ReceivedWebhookRequestType = WebhookRequestType.Preflight,
            });
        }
    }

    #endregion

    #region Bridge webhooks

    [Webhook("On analysis finished", typeof(AnalysisFinishedWebhookHandler),
        Description = "Triggers when project analysis is finished")]
    public async Task<WebhookResponse<AnalysisFinishedResponse>> OnAnalysisFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        try
        {
            var data = HandleBridgeWebhookRequest<AnalysisFinishedPayload>(request);

            if (data.Payload is null)
                throw new NullReferenceException("Payload received from bridge is null.");

            var result = new AnalysisFinishedResponse(data.Payload);

            if ((projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId) ||
               (!string.IsNullOrEmpty(projectOptionalRequest.CustomerNameContains) && !result.Customer.Name.Contains(projectOptionalRequest.CustomerNameContains)))
            {
                return PreflightResponse<AnalysisFinishedResponse>();
            }

            if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
                return PreflightResponse<AnalysisFinishedResponse>();

            return FlightResponse(result);
        }
        catch (Exception ex)
        {
            var queryParams = String.Join(", ", request.QueryParameters.Select(kv => $"{kv.Key}: {kv.Value}"));
            InvocationContext.Logger?.LogError($"[XTM_OnAnalysisFinished_Callback] {ex.Message}; URL params: {queryParams}; Body: {request.Body?.ToString()}; Stack trace: {ex.StackTrace?.ToString()}", []);
            throw;
        }
    }

    [Webhook("On workflow transition", typeof(WorkflowTransitionedWebhookHandler),
        Description = "Triggers when jobs move into active workflow steps or finish a selected step")]
    public async Task<WebhookResponse<WorkflowTransitionResponse>> OnWorkflowTransition(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] CustomerOptionalRequest customerOptionalRequest,
        [WebhookParameter] WorkflowStepOptionalRequest workflowOptionalRequest)
    {
        var flightOnEventTypes = workflowOptionalRequest.TriggerWhenStepFinished == true
            ? new List<string> { "FINISHED" }
            : new List<string> { "OPENED", "ACTIVE" };
        var data = HandleBridgeWebhookRequest<WorkflowTransitionPayload>(request);
        var result = new WorkflowTransitionResponse(data.Payload!);

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
            return PreflightResponse<WorkflowTransitionResponse>();

        if (customerOptionalRequest.CustomerId != null && customerOptionalRequest.CustomerId != result.CustomerId)
            return PreflightResponse<WorkflowTransitionResponse>();

        if (!string.IsNullOrEmpty(workflowOptionalRequest.WorkflowStep))
        {
            var anyMatch = data.Payload!.Events
                .Where(e => flightOnEventTypes.Contains(e.Type))
                .SelectMany(e => e.Tasks)
                .Any(t => IsMatchingWorkflowStep(t.Step, workflowOptionalRequest.WorkflowStep));

            if (!anyMatch)
                return PreflightResponse<WorkflowTransitionResponse>();
        }

        if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
            return PreflightResponse<WorkflowTransitionResponse>();

        return FlightResponse(result);
    }

    [Webhook("On job finished", typeof(JobFinishedWebhookHandler), Description = "Triggers when a job is finished")]
    public async Task<WebhookResponse<JobFinishedPayload>> OnJobFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest,
        [WebhookParameter] JobOptionalRequest jobOptionalRequest)
    {
        try
        {
            var data = HandleBridgeWebhookRequest<object>(request);
            var result = new JobFinishedPayload
            {
                ProjectId = data.Parameters["xtmProjectId"],
                CustomerId = data.Parameters["xtmCustomerId"],
                JobId = data.Parameters["xtmJobId"],
                Uuid = data.Parameters["xtmUuid"]
            };

            if (jobOptionalRequest.JobId != null && jobOptionalRequest.JobId != result.JobId)
                return PreflightResponse<JobFinishedPayload>();

            if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
                return PreflightResponse<JobFinishedPayload>();

            if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
                return PreflightResponse<JobFinishedPayload>();

            return FlightResponse(result);
        }
        catch (Exception ex)
        {
            var queryParams = String.Join(", ", request.QueryParameters.Select(kv => $"{kv.Key}: {kv.Value}"));
            InvocationContext.Logger?.LogError($"[XTM_OnJobFinished_Callback]  URL params: {queryParams}; Body: {request.Body?.ToString()}; Stack trace: {ex.StackTrace?.ToString()}", []);
            throw;
        }
    }

    [Webhook("On project created", typeof(ProjectCreatedWebhookHandler), Description = "Triggers when a project is created")]
    public async Task<WebhookResponse<ProjectCreatedPayload>> OnProjectCreated(WebhookRequest request)
    {
        var data = HandleBridgeWebhookRequest<object>(request);
        var result = new ProjectCreatedPayload
        {
            ProjectId = data.Parameters["xtmProjectId"],
            Uuid = data.Parameters["xtmUuid"]
        };

        if (!await IsAvailableToConnectionAndMatches(result.ProjectId, new ProjectOptionalRequest()))
            return PreflightResponse<ProjectCreatedPayload>();

        return FlightResponse(result);
    }

    [Webhook("On project accepted", typeof(ProjectAcceptedWebhookHandler),
        Description = "Triggers when a project is accepted")]
    public async Task<WebhookResponse<ProjectAcceptedPayload>> OnProjectAccepted(WebhookRequest request,
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
            return PreflightResponse<ProjectAcceptedPayload>();

        if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
            return PreflightResponse<ProjectAcceptedPayload>();

        return FlightResponse(result);
    }

    [Webhook("On project finished", typeof(ProjectFinishedWebhookHandler),
        Description = "Triggers when a project is finished")]
    public async Task<WebhookResponse<ProjectFinishedPayload>> OnProjectFinished(WebhookRequest request,
        [WebhookParameter] ProjectOptionalRequest projectOptionalRequest)
    {
        try
        {
            var data = HandleBridgeWebhookRequest<object>(request);
            var result = new ProjectFinishedPayload
            {
                ProjectId = data.Parameters["xtmProjectId"],
                CustomerId = data.Parameters["xtmCustomerId"],
                Uuid = data.Parameters["xtmUuid"]
            };

            if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
                return PreflightResponse<ProjectFinishedPayload>();

            if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
                return PreflightResponse<ProjectFinishedPayload>();

            return FlightResponse(result);
        }
        catch (Exception ex)
        {
            var queryParams = String.Join(", ", request.QueryParameters.Select(kv => $"{kv.Key}: {kv.Value}"));
            InvocationContext.Logger?.LogError($"[XTM_OnProjectFinished_Callback]  URL params: {queryParams}; Body: {request.Body?.ToString()}; Stack trace: {ex.StackTrace?.ToString()}", []);
            throw;
        }
    }

    [Webhook("On invoice status changed", typeof(InvoiceStatusChangedWebhookHandler),
        Description = "Triggers when an invoice status changes")]
    public async Task<WebhookResponse<InvoiceStatusChangedPayload>> OnInvoiceStatusChanged(WebhookRequest request,
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

        if (invoiceStatusChangedRequest.InvoiceId != null && invoiceStatusChangedRequest.InvoiceId != result.Uuid)
            return PreflightResponse<InvoiceStatusChangedPayload>();

        if (projectOptionalRequest.ProjectId != null && projectOptionalRequest.ProjectId != result.ProjectId)
            return PreflightResponse<InvoiceStatusChangedPayload>();

        if (!await IsAvailableToConnectionAndMatches(result.ProjectId, projectOptionalRequest))
            return PreflightResponse<InvoiceStatusChangedPayload>();

        return FlightResponse(result);
    }

    #endregion

    #region Utils

    private BridgeWebhookPayload<T> HandleBridgeWebhookRequest<T>(WebhookRequest request)
    {
        var parameters = new Dictionary<string, string>();
        T? payload = default;

        if (request.QueryParameters != null)
        {
            foreach (var (key, value) in request.QueryParameters)
            {
                parameters.Add(key, value);
            }
        }

        var body = request.Body?.ToString();

        if (!string.IsNullOrEmpty(body))
        {
            var bridgeWebhookPayload = JsonConvert.DeserializeObject<BridgeWebhookPayload<T>>(body)
                ?? throw new Exception($"Failed to deserialize webhook payload. Body: {body}");

            payload = bridgeWebhookPayload.Payload;

            if (parameters.Count == 0)
            {
                foreach (var (key, value) in bridgeWebhookPayload.Parameters)
                {
                    parameters.Add(key, value);
                }
            }
        }

        return new BridgeWebhookPayload<T>
        {
            Parameters = parameters,
            Payload = payload
        };
    }

    private static bool IsMatchingWorkflowStep(WorkflowStepPayload step, string selectedStep)
    {
        if (string.IsNullOrWhiteSpace(selectedStep))
            return false;

        return new[] { step.WorkflowStepName, step.WorkflowStep }
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Any(candidate => NormalizeWorkflowStepName(candidate)
                .StartsWith(NormalizeWorkflowStepName(selectedStep), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeWorkflowStepName(string value) =>
        string.Concat(value.Where(c => !char.IsWhiteSpace(c)));

    private static Task<WebhookResponse<T>> GetPreflightResponse<T>()
        where T : class
    {
        return Task.FromResult(PreflightResponse<T>());
    }

    private static WebhookResponse<T> PreflightResponse<T>()
        where T : class
    {
        return new WebhookResponse<T>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = null,
            ReceivedWebhookRequestType = WebhookRequestType.Preflight
        };
    }

    private static WebhookResponse<T> FlightResponse<T>(T result)
        where T : class
    {
        return new WebhookResponse<T>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = result
        };
    }

    private async Task<bool> IsAvailableToConnectionAndMatches(string? projectId, ProjectOptionalRequest projectFilter)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return false;

        var project = await Client.GetXtmOrDefaultWhenUnavailable<FullProject>(
            $"{ApiEndpoints.Projects}/{projectId}", Creds);

        return project is not null && MatchesNameFilters(project, projectFilter);
    }

    private bool ProjectFilter(string projectId, string projectNameContains, string customerNameContains)
    {
        if (String.IsNullOrEmpty(projectNameContains) && String.IsNullOrEmpty(customerNameContains))
            return true;

        var project = Client.GetXtmOrDefaultWhenUnavailable<FullProject>(
            $"{ApiEndpoints.Projects}/{projectId}", Creds).GetAwaiter().GetResult();

        return project is not null && MatchesNameFilters(project, new ProjectOptionalRequest
        {
            ProjectNameContains = projectNameContains,
            CustomerNameContains = customerNameContains
        });
    }

    private static bool MatchesNameFilters(FullProject project, ProjectOptionalRequest projectFilter)
    {
        if (!String.IsNullOrEmpty(projectFilter.ProjectNameContains) && !project.Name.Contains(projectFilter.ProjectNameContains))
            return false;

        if (!String.IsNullOrEmpty(projectFilter.CustomerNameContains) && !project.CustomerName.Contains(projectFilter.CustomerNameContains))
            return false;

        return true;
    }

    #endregion
}
