using System.Linq;
using System.Net.Mime;
using System.Text;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.TranslationMemory;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.TranslationMemory;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml.Office2016.Excel;
using Newtonsoft.Json;
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


    [Action("Import TM file", Description = "Import translation memory file")]
    public async Task<ImportTMResponse> ImportTMFile([ActionParameter] ImportTMRequest request)
    {
        var baseUrl = Creds.Get(CredsNames.Url).Value;
        var url = baseUrl + $"{ApiEndpoints.TMFiles}/import";
        var token = await Client.GetToken(Creds);

        var parameters = new Dictionary<string, object>
            {
                { "customerId", int.Parse(request.CustomerId) },
                { "importProjectName", request.ImportProjectName },
                { "sourceLanguage", request.SourceLanguage },
                { "targetLanguage", request.TargetLanguage }
            };

        if (!string.IsNullOrEmpty(request.TmStatus))
            parameters.Add("tmStatus", request.TmStatus);
        if (!string.IsNullOrEmpty(request.TmStatusImportType))
            parameters.Add("tmStatusImportType", request.TmStatusImportType);
        if (!string.IsNullOrEmpty(request.WhitespacesFormattingType))
            parameters.Add("whitespacesFormattingType", request.WhitespacesFormattingType);
        if (!string.IsNullOrEmpty(request.AltTransElementsImport))
            parameters.Add("altTransElementsImport", request.AltTransElementsImport);
        if (!string.IsNullOrEmpty(request.SegmentsImportType))
            parameters.Add("segmentsImportType", request.SegmentsImportType);
        if (!string.IsNullOrEmpty(request.BilingualTerminologyAction))
            parameters.Add("bilingualTerminologyAction", request.BilingualTerminologyAction);

        if (!string.IsNullOrEmpty(request.TagGroupIds) && request.TagIds != null && request.TagIds.Any())
        {
            if (long.TryParse(request.TagGroupIds, out var groupId))
            {
                var tagsList = request.TagIds
                    .Where(x => long.TryParse(x, out _))
                    .Select(x => new { id = long.Parse(x) })
                    .ToList();

                var tagGroups = new[] { new { id = groupId, tags = tagsList } };
                var tagGroupsJson = JsonConvert.SerializeObject(tagGroups);
                parameters.Add("tagGroups", tagGroupsJson);
            }
        }

        var xtmRequest = new XTMRequest(new()
        {
            Url =  url,
            Method = Method.Post
        }, token);

        foreach (var param in parameters)
        {
            xtmRequest.AddParameter(param.Key, param.Value, ParameterType.GetOrPost);
        }

        var fileStream = await _fileManagementClient.DownloadAsync(request.File);
        var fileBytes = await fileStream.GetByteData();

        xtmRequest.AddFile("file", fileBytes, request.File.Name);
        xtmRequest.AlwaysMultipartFormData = true;

        try
        {
            var response = await Client.ExecuteXtm<IEnumerable<ImportTMResponse>>(xtmRequest);
            return response.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException(ex.Message);
        }
    }

    #endregion
}