using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Templates;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Actions
{
    [ActionList]
    public class TemplateActions : XtmInvocable
    {
        public TemplateActions(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        #region Actions

        [Action("Get template by ID", Description = "Get template by ID")]
        public async Task<ProjectTemplate> GetTemplate([ActionParameter, Display("Template ID")] string templateId)
        {
            return await Client.ExecuteXtmWithJson<ProjectTemplate>($"{ApiEndpoints.Projects}{ApiEndpoints.Templates}/{templateId}",
                               RestSharp.Method.Get,
                               null,
                               Creds);
        }

        #endregion
    }
}
