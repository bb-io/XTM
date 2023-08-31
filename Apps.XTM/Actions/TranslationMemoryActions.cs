using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.TranslationMemory;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.TranslationMemory;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class TranslationMemoryActions : XtmInvocable
{
    public TranslationMemoryActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    #region Actions

    [Action("Generate TM file", Description = "Generate translation memory file")]
    public Task<TranslationMemoryResponse> GenerateTMFile([ActionParameter] GenerateTMRequest input)
    {
        return Client.ExecuteXtmWithJson<TranslationMemoryResponse>($"{ApiEndpoints.TMFiles}/generate",
            Method.Post,
            input,
            Creds);
    }

    [Action("Download TM file", Description = "Download generated translation memory file")]
    public async Task<FileResponse> DownloadTMFile([ActionParameter] [Display("File ID")] string fileId)
    {
        var response = await Client.ExecuteXtmWithJson($"{ApiEndpoints.TMFiles}/{fileId}/download",
            Method.Get,
            null,
            Creds);

        return new(new(response.RawBytes)
        {
            Name = $"TMFile-{fileId}",
            ContentType = response.ContentType ?? MediaTypeNames.Application.Octet
        });
    }

    #endregion
}