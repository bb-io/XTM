using System.Net.Mime;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.TranslationMemory;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.TranslationMemory;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class TranslationMemoryActions : XtmInvocable
{
    private readonly IFileManagementClient _fileManagementClient;
    
    public TranslationMemoryActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
        : base(invocationContext)
    {
        _fileManagementClient = fileManagementClient;
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

        using var stream = new MemoryStream(response.RawBytes);
        var file = await _fileManagementClient.UploadAsync(stream,
            response.ContentType ?? MediaTypeNames.Application.Octet, $"TMFile-{fileId}");
        return new(file);
    }

    #endregion
}