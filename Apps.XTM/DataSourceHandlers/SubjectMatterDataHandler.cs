﻿using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.SubjectMatters;
using Apps.XTM.Models.Response.Templates;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.DataSourceHandlers;

public class SubjectMatterDataHandler : XtmInvocable, IAsyncDataSourceHandler
{
    public SubjectMatterDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var response = await Client.ExecuteXtmWithJson<List<SubjectMatterResponse>>(ApiEndpoints.SubjectMatters,
            Method.Get,
            null,
            Creds);

        return response
            .Where(x => context.SearchString == null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToDictionary(x => x.Id, x => x.Name);
    }
}