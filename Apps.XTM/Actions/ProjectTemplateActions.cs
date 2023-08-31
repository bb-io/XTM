using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Templates;
using Apps.XTM.Models.Response.Templates;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class ProjectTemplateActions : XtmInvocable
{
    public ProjectTemplateActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("Create project template", Description = "Create a new project template")]
    public Task<SimpleProjectTemplateResponse> CreateProjectTemplate([ActionParameter] CreateProjectTemplateRequest input)
    {
        if (input.TemplateType == "CUSTOMER" && input.CustomerId is null)
            throw new("You must specify customer ID for creating Customer templates");
        
        return Client.ExecuteXtmWithJson<SimpleProjectTemplateResponse>(ApiEndpoints.Templates,
            Method.Post,
            input,
            Creds);
    }    
    
    [Action("List project templates", Description = "List all project templates")]
    public async Task<ListTemplatesResponse> ListProjectTemplates()
    {
        var endpoint = $"{ApiEndpoints.Projects}/templates";
        var response = await Client.ExecuteXtmWithJson<List<ProjectTemplate>>(endpoint,
            Method.Get,
            null,
            Creds);

        return new(response);
    }
}