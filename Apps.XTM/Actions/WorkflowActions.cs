﻿using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

public class WorkflowActions : XtmInvocable
{
    public WorkflowActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    #region Actions

    [Action("List workflows", Description = "List all workflows")]
    public async Task<AllWorkflowsResponse> ListWorkflows()
    {
        var response = await Client.ExecuteXtmWithJson<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}",
            Method.Get,
            null,
            Creds);

        return new(response);
    }
    
    [Action("List workflow steps", Description = "List all workflow steps")]
    public async Task<AllWorkflowStepsResponse> ListWorkflowSteps()
    {
        var endpoint = $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}";
        var response = await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(endpoint,
            Method.Get,
            null,
            Creds);

        return new(response);
    }

    #endregion
}