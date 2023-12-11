using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Projects;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class ProjectTargetLanguageDataSourceHandler : XtmInvocable, IAsyncDataSourceHandler
{
    private readonly ProjectRequest _project;
    
    public ProjectTargetLanguageDataSourceHandler(InvocationContext invocationContext, 
        [ActionParameter] ProjectRequest project) : base(invocationContext)
    {
        _project = project;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, 
        CancellationToken cancellationToken)
    {
        if (_project.ProjectId == null)
            throw new Exception("Please specify project first.");
        
        var response = await Client.ExecuteXtmWithJson<FullProject>($"{ApiEndpoints.Projects}/{_project.ProjectId}",
            Method.Get,
            null,
            Creds);

        var languages = Languages.All
            .Where(kvp => response.TargetLanguages.Contains(kvp.Key))
            .Where(language => context.SearchString == null
                               || language.Value.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return languages;
    }
}