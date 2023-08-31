using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.SubjectMatters;
using Apps.XTM.Models.Response.SubjectMatters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class SubjectMatterActions : XtmInvocable
{
    public SubjectMatterActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List subject matters", Description = "List all subject matters")]
    public async Task<ListSubjectMattersResponse> ListSubjectMatters(
        [ActionParameter] ListSubjectMattersRequest input,
        [ActionParameter] [Display("Subject matter IDs")] IEnumerable<string>? subjectMatterIds)
    {
        var endpoint = ApiEndpoints.SubjectMatters;

        if (subjectMatterIds != null)
            endpoint += $"?{string.Join("&", subjectMatterIds.Select(x => $"ids={x}"))}";
        
        var response = await Client.ExecuteXtmWithJson<List<SubjectMatterResponse>>(endpoint.WithQuery(input),
            Method.Get,
            null,
            Creds);

        return new(response);
    }
}