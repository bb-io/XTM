using Apps.XTM.Constants;
using Apps.XTM.Models.Request.TranslationMemory;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.TranslationMemory;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class TranslationMemoryActions
{
    #region Fields

    private static XTMClient _client;

    #endregion

    #region Constructors

    static TranslationMemoryActions()
    {
        _client = new();
    }

    #endregion

    #region Actions

    [Action("Generate TM file", Description = "Generate translation memory file")]
    public Task<TranslationMemoryResponse> GenerateTMFile(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] GenerateTMRequest input)
    {
        return _client.ExecuteXtm<TranslationMemoryResponse>($"{ApiEndpoints.TMFiles}/generate",
            Method.Post,
            bodyObj: input,
            authenticationCredentialsProviders.ToArray());
    }

    [Action("Download TM file", Description = "Download generated translation memory file")]
    public async Task<FileResponse> DownloadTMFile(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] [Display("File id")] int fileId)
    {
        var response = await _client.ExecuteXtm($"{ApiEndpoints.TMFiles}/{fileId}/download",
            Method.Get,
            bodyObj: null,
            authenticationCredentialsProviders.ToArray());

        return new(response.RawBytes);
    }

    #endregion
}