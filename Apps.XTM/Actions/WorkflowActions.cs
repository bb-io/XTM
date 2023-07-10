using Apps.XTM.Constants;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;

namespace Apps.XTM.Actions;

public class WorkflowActions
{
    #region Fields

    private static XTMClient _client;

    #endregion

    #region Constructors

    static WorkflowActions()
    {
        _client = new();
    }

    #endregion

    #region Actions

    [Action("List workflows", Description = "List all workflows")]
    public async Task<AllWorkflowsResponse> ListWorkflows(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
    {
        var response = await _client.ExecuteXtm<List<WorkflowResponse>>($"{ApiEndpoints.Workflows}",
            Method.Get,
            bodyObj: null,
            authenticationCredentialsProviders.ToArray());

        return new(response);
    }

    #endregion
}