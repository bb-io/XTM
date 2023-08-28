using Apps.XTM.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.RestUtilities;
using Apps.XTM.Constants;
using Apps.XTM.Models.Response.Projects;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers
{
    internal class ProjectTemplateDataHandler : BaseInvocable, IAsyncDataSourceHandler
    {
        private IEnumerable<AuthenticationCredentialsProvider> Creds =>
            InvocationContext.AuthenticationCredentialsProviders;

        private static XTMClient _client;

        public ProjectTemplateDataHandler(InvocationContext invocationContext) : base(invocationContext)
        {
            _client = new();
        }

        public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
        {
            var templates = await _client.ExecuteXtm<List<ProjectTemplate>>($"{ApiEndpoints.Projects}/templates",
                Method.Get,
                bodyObj: null,
                Creds.ToArray());

            return templates
                .Where(x => context.SearchString == null || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToDictionary(x => x.Id.ToString(), x => x.Name);
        }
    }
}
