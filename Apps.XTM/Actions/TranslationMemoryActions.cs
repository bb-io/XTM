using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Linq;
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

    [Action("Generate TM file", Description = "Generate a translation memory file")]
    public Task<TranslationMemoryResponse> GenerateTMFile([ActionParameter] GenerateTMRequest input)
    {
        input.fileType = string.IsNullOrEmpty(input.fileType) ? "TMX" : input.fileType;
        return Client.ExecuteXtmWithJson<TranslationMemoryResponse>($"{ApiEndpoints.TMFiles}/generate",
            Method.Post,
            input,
            Creds);
    }

    [Action("Download TM file", Description = "Download a generated translation memory file")]
    public async Task<FileResponse> DownloadTMFile([ActionParameter] [Display("File ID")] string fileId)
    {
        var response = await Client.ExecuteXtmWithJson($"{ApiEndpoints.TMFiles}/{fileId}/download",
            Method.Get,
            null,
            Creds);

        using var stream = new MemoryStream(response.RawBytes);
        var file = await _fileManagementClient.UploadAsync(stream,
            response.ContentType ?? MediaTypeNames.Application.Octet, $"TMFile-{fileId}.zip");
        return new(file);
    }


    [Action("Import TM file", Description = "Import a translation memory file")]
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

    [Action("Generate TMX for tagging", Description = "Generates a TMX file from a scored XLIFF file. Only segments with a quality score equal to or above the threshold are included. Each translation unit is tagged to identify it as TAUS-origin.")]
    public async Task<GenerateTmxFromXliffResponse> GenerateTmxFromXliff(
        [ActionParameter] GenerateTmxFromXliffRequest input)
    {
        var tag = input.Tag ?? "auto_approved";
        var tagGroup = input.TagGroup ?? "QE";

        var (doc, srcLang, trgLang) = await ParseXliffAsync(input.File);
        var tuElements = BuildTuElements(doc, srcLang, trgLang, tag, tagGroup);
        var tmxDoc = BuildTmxDocument(srcLang, tuElements);
        var outputName = Path.GetFileNameWithoutExtension(input.File.Name) + ".tmx";
        var fileRef = await UploadTmxAsync(tmxDoc, outputName);

        return new GenerateTmxFromXliffResponse
        {
            File = fileRef,
            SegmentsTagged = tuElements.Count,
            TagGroupUsed = tagGroup,
            TagUsed = tag
        };
    }

    private async Task<(XDocument doc, string srcLang, string trgLang)> ParseXliffAsync(
        Blackbird.Applications.Sdk.Common.Files.FileReference fileRef)
    {
        var stream = await _fileManagementClient.DownloadAsync(fileRef);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        var doc = XDocument.Load(memoryStream);
        var root = doc.Root!;

        var srcLang = root.Attribute("srcLang")?.Value ?? "en-US";
        var trgLang = root.Attribute("trgLang")?.Value
            ?? throw new PluginMisconfigurationException("The XLIFF file is missing the 'trgLang' attribute on the root element.");

        return (doc, srcLang, trgLang);
    }

    private static List<XElement> BuildTuElements(
        XDocument doc, string srcLang, string trgLang, string tag, string tagGroup)
    {
        var xliffNs = XNamespace.Get("urn:oasis:names:tc:xliff:document:2.2");
        var itsNs = XNamespace.Get("http://www.w3.org/2005/11/its");

        return doc.Root!
            .Descendants(xliffNs + "unit")
            .Where(unit => MeetsQualityThreshold(unit, itsNs))
            .Select(unit => TryExtractSegmentTexts(unit, xliffNs))
            .OfType<(string src, string tgt)>()
            .Select(texts => BuildTuElement(texts.src, texts.tgt, srcLang, trgLang, tag, tagGroup))
            .ToList();
    }

    private static bool MeetsQualityThreshold(XElement unit, XNamespace itsNs)
    {
        var scoreAttr = unit.Attribute(itsNs + "locQualityRatingScore");
        var thresholdAttr = unit.Attribute(itsNs + "locQualityRatingScoreThreshold");

        if (scoreAttr == null || thresholdAttr == null)
            return false;

        var style = System.Globalization.NumberStyles.Float;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return double.TryParse(scoreAttr.Value, style, culture, out var score)
            && double.TryParse(thresholdAttr.Value, style, culture, out var threshold)
            && score >= threshold;
    }

    private static (string src, string tgt)? TryExtractSegmentTexts(XElement unit, XNamespace xliffNs)
    {
        var segment = unit.Element(xliffNs + "segment");
        if (segment == null)
            return null;

        var src = ExtractPlainText(segment.Element(xliffNs + "source")).Trim();
        var tgt = ExtractPlainText(segment.Element(xliffNs + "target")).Trim();

        return string.IsNullOrEmpty(src) || string.IsNullOrEmpty(tgt) ? null : (src, tgt);
    }

    private static XElement BuildTuElement(
        string src, string tgt, string srcLang, string trgLang, string tag, string tagGroup)
    {
        return new XElement("tu",
            new XElement("prop", new XAttribute("type", tagGroup), tag),
            new XElement("tuv", new XAttribute(XNamespace.Xml + "lang", srcLang), new XElement("seg", src)),
            new XElement("tuv", new XAttribute(XNamespace.Xml + "lang", trgLang), new XElement("seg", tgt))
        );
    }

    private static XDocument BuildTmxDocument(string srcLang, IEnumerable<XElement> tuElements)
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("tmx", null, "tmx14.dtd", null),
            new XElement("tmx", new XAttribute("version", "1.4"),
                new XElement("header",
                    new XAttribute("creationtool", "Blackbird"),
                    new XAttribute("creationtoolversion", "1.0"),
                    new XAttribute("segtype", "sentence"),
                    new XAttribute("o-tmf", "XTM"),
                    new XAttribute("adminlang", "en-US"),
                    new XAttribute("srclang", srcLang),
                    new XAttribute("datatype", "plaintext")),
                new XElement("body", tuElements)
            )
        );
    }

    private async Task<Blackbird.Applications.Sdk.Common.Files.FileReference> UploadTmxAsync(
        XDocument tmxDoc, string outputName)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false)
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
            tmxDoc.Save(writer);

        ms.Position = 0;
        return await _fileManagementClient.UploadAsync(ms, "application/x-tmx+xml", outputName);
    }

    private static string ExtractPlainText(XElement? element)
    {
        if (element == null)
            return string.Empty;
        return string.Concat(element.DescendantNodes().OfType<XText>().Select(t => t.Value));
    }

    #endregion
}
