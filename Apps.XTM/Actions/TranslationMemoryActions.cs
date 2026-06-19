using System.Buffers.Binary;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.TranslationMemory;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Tag;
using Apps.XTM.Models.Response.TranslationMemory;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.XTM.Actions;

[ActionList]
public class TranslationMemoryActions : XtmInvocable
{
    private const int PollDelayMs = 5000;
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
        var tagGroups = BuildSingleGroupTagPayload(request.TagGroupIds, request.TagIds);

        return await ImportTmFileInternal(
            request.File,
            request.CustomerId,
            request.ImportProjectName,
            request.SourceLanguage,
            request.TargetLanguage,
            request.TmStatus,
            request.TmStatusImportType,
            request.WhitespacesFormattingType,
            request.AltTransElementsImport,
            request.SegmentsImportType,
            request.BilingualTerminologyAction,
            tagGroups);
    }

    [Action("Tag TM segments based on edits", Description = "Export a TMX, keep only segments created by selected users that were not later changed by trusted users, and re-import the filtered TMX with tags.")]
    public async Task<TagTmSegmentsBasedOnEditsResponse> TagTmSegmentsBasedOnEdits(
        [ActionParameter] TagTmSegmentsBasedOnEditsRequest input)
    {
        ValidateTaggingRequest(input);

        var exportRequest = BuildTmExportRequest(input);
        var exportResponse = await Client.ExecuteXtmWithJson<TranslationMemoryResponse>(
            $"{ApiEndpoints.TMFiles}/generate",
            Method.Post,
            exportRequest,
            Creds);

        await PollTmFileStatusAsync(exportResponse.FileId);

        var downloadResponse = await Client.ExecuteXtmWithJson(
            $"{ApiEndpoints.TMFiles}/{exportResponse.FileId}/download",
            Method.Get,
            null,
            Creds);

        var (entryName, tmxBytes) = ExtractSingleTmxFromZip(downloadResponse.RawBytes ?? []);
        var filteredOutputName = $"{Path.GetFileNameWithoutExtension(entryName)}-filtered.tmx";

        if (TryExtractPlainTextMessage(tmxBytes, out var exportedMessage))
        {
            if (exportedMessage.Contains("No TM matching given criteria was found.", StringComparison.OrdinalIgnoreCase))
            {
                var emptyFile = await UploadTmxAsync(BuildEmptyTmxDocument(input.SourceLanguage), filteredOutputName);

                return new()
                {
                    ExportFileId = exportResponse.FileId,
                    FilteredTmxFile = emptyFile,
                    ExportedSegmentsScanned = 0,
                    SegmentsMatched = 0,
                    SegmentsSkipped = 0,
                    ImportStatus = input.DryRun == true ? "DRY_RUN" : "SKIPPED_NO_MATCHES"
                };
            }

            throw new PluginApplicationException(
                $"The exported TM payload is not a valid TMX document. XTM returned: {exportedMessage}");
        }

        var filterResult = FilterTmxByEdits(
            tmxBytes,
            input.CreatedByUserIds.ToHashSet(StringComparer.Ordinal),
            (input.UntrustedUserIds ?? []).ToHashSet(StringComparer.Ordinal));

        var filteredFile = await UploadTmxAsync(filterResult.Bytes, filteredOutputName);

        if (input.DryRun == true || filterResult.MatchedSegments == 0)
        {
            return new()
            {
                ExportFileId = exportResponse.FileId,
                FilteredTmxFile = filteredFile,
                ExportedSegmentsScanned = filterResult.ScannedSegments,
                SegmentsMatched = filterResult.MatchedSegments,
                SegmentsSkipped = filterResult.ScannedSegments - filterResult.MatchedSegments,
                ImportStatus = input.DryRun == true ? "DRY_RUN" : "SKIPPED_NO_MATCHES"
            };
        }

        var tagGroups = await BuildTagPayloadByGroupAsync(input.TagIds!);
        var importResponse = await ImportTmFileInternal(
            filteredFile,
            input.CustomerId,
            input.ImportProjectName,
            input.SourceLanguage,
            input.TargetLanguage,
            null,
            "FROM_FILE",
            "KEEP_ALL_WHITESPACES",
            "NONE",
            "SOURCE_AND_TARGET",
            "NONE",
            tagGroups);

        var importStatus = await PollTmImportStatusAsync(importResponse.FileId);

        return new()
        {
            ExportFileId = exportResponse.FileId,
            FilteredTmxFile = filteredFile,
            ExportedSegmentsScanned = filterResult.ScannedSegments,
            SegmentsMatched = filterResult.MatchedSegments,
            SegmentsSkipped = filterResult.ScannedSegments - filterResult.MatchedSegments,
            ImportedFileId = importResponse.FileId,
            ImportedFileName = importResponse.FileName,
            ImportStatus = importStatus.Status
        };
    }

    // [Action("Generate TMX for tagging", Description = "Generates a TMX file from a scored XLIFF file. Only segments with a quality score equal to or above the threshold are included. Each translation unit is tagged to identify it as TAUS-origin.")]
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

    #endregion

    #region New tagging flow

    private void ValidateTaggingRequest(TagTmSegmentsBasedOnEditsRequest input)
    {
        if (input.CreatedByUserIds == null || !input.CreatedByUserIds.Any())
            throw new PluginMisconfigurationException("At least one 'Created by user ID' must be provided.");

        if (input.DryRun != true && string.IsNullOrWhiteSpace(input.ImportProjectName))
            throw new PluginMisconfigurationException("'Import project name' is required when dry run is disabled.");

        if (input.DryRun != true && (input.TagIds == null || !input.TagIds.Any()))
            throw new PluginMisconfigurationException("At least one tag ID must be provided when dry run is disabled.");
    }

    private object BuildTmExportRequest(TagTmSegmentsBasedOnEditsRequest input)
    {
        return new
        {
            fileType = "TMX",
            customerId = int.Parse(input.CustomerId),
            projectId = ParseNullableLong(input.ProjectId),
            sourceLanguage = input.SourceLanguage,
            targetLanguage = input.TargetLanguage,
            createdDateFrom = FormatIsoDateTime(input.CreatedDateFrom),
            createdDateTo = FormatIsoDateTime(input.CreatedDateTo),
            changedDateFrom = FormatIsoDateTime(input.ChangedDateFrom),
            changedDateTo = FormatIsoDateTime(input.ChangedDateTo),
            includeReverseMemory = input.IncludeReverseMemory == true ? "INCLUDE" : "DO_NOT_INCLUDE",
            approvalStatus = input.ApprovalStatus
        };
    }

    private async Task PollTmFileStatusAsync(string fileId)
    {
        TMFileStatusResponse statusResponse;

        do
        {
            statusResponse = await Client.ExecuteXtmWithJson<TMFileStatusResponse>(
                $"{ApiEndpoints.TMFiles}/{fileId}/status",
                Method.Get,
                null,
                Creds);

            if (statusResponse.Status == "ERROR")
                throw new PluginApplicationException(
                    $"TM export failed for file ID {fileId}. {statusResponse.Message}".Trim());

            if (statusResponse.Status != "FINISHED")
                await Task.Delay(PollDelayMs);
        } while (statusResponse.Status != "FINISHED");
    }

    private async Task<TMImportStatusResponse> PollTmImportStatusAsync(string fileId)
    {
        TMImportStatusResponse statusResponse;

        do
        {
            var statusResponses = await Client.ExecuteXtmWithJson<List<TMImportStatusResponse>>(
                $"{ApiEndpoints.TMFiles}/import/status?fileIds={fileId}",
                Method.Get,
                null,
                Creds);

            statusResponse = statusResponses.FirstOrDefault(x => x.FileId == fileId)
                ?? statusResponses.FirstOrDefault()
                ?? throw new PluginApplicationException(
                    $"TM import status polling returned no entries for file ID {fileId}.");

            if (statusResponse.Status == "ERROR")
                throw new PluginApplicationException(
                    $"TM import failed for file ID {fileId}. {statusResponse.ExtractionError ?? statusResponse.BilingualTermExtractionError}".Trim());

            if (statusResponse.Status != "DONE")
                await Task.Delay(PollDelayMs);
        } while (statusResponse.Status != "DONE");

        return statusResponse;
    }

    private FilteredTmxResult FilterTmxByEdits(
        byte[] tmxBytes,
        HashSet<string> createdByUserIds,
        HashSet<string> untrustedUserIds)
    {
        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            IgnoreWhitespace = false
        };

        var writerSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false)
        };

        var scannedSegments = 0;
        var matchedSegments = 0;

        using var inputStream = new MemoryStream(tmxBytes);
        using var outputStream = new MemoryStream();
        using var reader = XmlReader.Create(inputStream, readerSettings);
        using var writer = XmlWriter.Create(outputStream, writerSettings);

        writer.WriteStartDocument();
        writer.WriteDocType("tmx", null, "tmx14.dtd", null);

        reader.MoveToContent();
        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "tmx")
            throw new PluginApplicationException("The exported TMX has an unexpected root element.");

        writer.WriteStartElement("tmx");
        CopyAttributes(reader, writer);

        reader.ReadStartElement("tmx");
        reader.MoveToContent();

        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "header")
            throw new PluginApplicationException("The exported TMX is missing the <header> element.");

        var header = (XElement)XNode.ReadFrom(reader);
        header.WriteTo(writer);

        reader.MoveToContent();
        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "body")
            throw new PluginApplicationException("The exported TMX is missing the <body> element.");

        writer.WriteStartElement("body");
        CopyAttributes(reader, writer);

        if (reader.IsEmptyElement)
        {
            reader.ReadStartElement("body");
        }
        else
        {
            reader.ReadStartElement("body");

            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "body")
                {
                    reader.ReadEndElement();
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tu")
                {
                    var tu = (XElement)XNode.ReadFrom(reader);
                    scannedSegments++;

                    if (ShouldKeepTu(tu, createdByUserIds, untrustedUserIds))
                    {
                        matchedSegments++;
                        tu.WriteTo(writer);
                    }

                    continue;
                }

                if (reader.NodeType is XmlNodeType.Comment or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace)
                {
                    writer.WriteNode(reader, false);
                    continue;
                }

                reader.Read();
            }
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();

        return new FilteredTmxResult
        {
            Bytes = outputStream.ToArray(),
            ScannedSegments = scannedSegments,
            MatchedSegments = matchedSegments
        };
    }

    private static bool ShouldKeepTu(
        XElement tu,
        HashSet<string> createdByUserIds,
        HashSet<string> untrustedUserIds)
    {
        var creationId = tu.Attribute("creationid")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(creationId))
            return false;

        if (!createdByUserIds.Contains(creationId))
            return false;

        var changeId = tu.Attribute("changeid")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(changeId))
            return true;

        if (changeId == creationId)
            return true;

        return untrustedUserIds.Contains(changeId);
    }

    private static (string EntryName, byte[] TmxBytes) ExtractSingleTmxFromZip(byte[] zipBytes)
    {
        if (zipBytes.Length == 0)
            throw new PluginApplicationException("The downloaded TM export ZIP is empty.");

        try
        {
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            var entry = archive.Entries.FirstOrDefault(x =>
                            x.FullName.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
                        ?? archive.Entries.FirstOrDefault();

            if (entry == null)
                throw new PluginApplicationException("The downloaded TM export ZIP does not contain any files.");

            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);

            return (entry.Name, memoryStream.ToArray());
        }
        catch (InvalidDataException)
        {
            return ExtractSingleTmxFromBrokenZip(zipBytes);
        }
    }

    private static (string EntryName, byte[] TmxBytes) ExtractSingleTmxFromBrokenZip(byte[] zipBytes)
    {
        if (ReadUInt32(zipBytes, 0) != 0x04034b50)
            throw new PluginApplicationException("The downloaded TM export ZIP has an invalid local header.");

        var flags = ReadUInt16(zipBytes, 6);
        var compressionMethod = ReadUInt16(zipBytes, 8);
        var compressedSize32 = ReadUInt32(zipBytes, 18);
        var uncompressedSize32 = ReadUInt32(zipBytes, 22);
        var fileNameLength = ReadUInt16(zipBytes, 26);
        var extraFieldLength = ReadUInt16(zipBytes, 28);

        var fileNameOffset = 30;
        var extraFieldOffset = fileNameOffset + fileNameLength;
        var dataOffset = extraFieldOffset + extraFieldLength;

        if (zipBytes.Length < dataOffset)
            throw new PluginApplicationException("The downloaded TM export ZIP is truncated.");

        var entryNameBytes = zipBytes.AsSpan(fileNameOffset, fileNameLength).ToArray();
        var entryName = (flags & 0x0800) != 0
            ? Encoding.UTF8.GetString(entryNameBytes)
            : Encoding.Default.GetString(entryNameBytes);

        long compressedSize = compressedSize32;
        long uncompressedSize = uncompressedSize32;

        if (compressedSize32 == uint.MaxValue || uncompressedSize32 == uint.MaxValue)
        {
            var extraFieldSpan = zipBytes.AsSpan(extraFieldOffset, extraFieldLength);
            var cursor = 0;

            while (cursor + 4 <= extraFieldSpan.Length)
            {
                var headerId = BinaryPrimitives.ReadUInt16LittleEndian(extraFieldSpan.Slice(cursor, 2));
                var dataSize = BinaryPrimitives.ReadUInt16LittleEndian(extraFieldSpan.Slice(cursor + 2, 2));
                cursor += 4;

                if (cursor + dataSize > extraFieldSpan.Length)
                    break;

                if (headerId == 0x0001)
                {
                    var zip64Cursor = cursor;

                    if (uncompressedSize32 == uint.MaxValue)
                    {
                        uncompressedSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(extraFieldSpan.Slice(zip64Cursor, 8));
                        zip64Cursor += 8;
                    }

                    if (compressedSize32 == uint.MaxValue)
                        compressedSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(extraFieldSpan.Slice(zip64Cursor, 8));

                    break;
                }

                cursor += dataSize;
            }
        }

        if (compressedSize <= 0 || dataOffset + compressedSize > zipBytes.Length)
            throw new PluginApplicationException("The downloaded TM export ZIP does not expose a valid TMX payload size.");

        var compressedData = zipBytes.AsSpan(dataOffset, (int)compressedSize).ToArray();

        return compressionMethod switch
        {
            0 => (entryName, compressedData),
            8 => (entryName, InflateRawDeflate(compressedData, uncompressedSize)),
            _ => throw new PluginApplicationException(
                $"Unsupported compression method '{compressionMethod}' in the downloaded TM export ZIP.")
        };
    }

    private static byte[] InflateRawDeflate(byte[] compressedData, long expectedSize)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = expectedSize > 0 ? new MemoryStream((int)Math.Min(expectedSize, int.MaxValue)) : new MemoryStream();
        deflateStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static void CopyAttributes(XmlReader reader, XmlWriter writer)
    {
        if (!reader.HasAttributes)
            return;

        while (reader.MoveToNextAttribute())
            writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);

        reader.MoveToElement();
    }

    private static uint ReadUInt32(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    private static ushort ReadUInt16(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    private static long? ParseNullableLong(string? value)
        => long.TryParse(value, out var parsed) ? parsed : null;

    private static string? FormatIsoDateTime(DateTime? value)
        => value?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    #endregion

    #region Shared import helpers

    private async Task<ImportTMResponse> ImportTmFileInternal(
        FileReference file,
        string customerId,
        string importProjectName,
        string sourceLanguage,
        string targetLanguage,
        string? tmStatus,
        string? tmStatusImportType,
        string? whitespacesFormattingType,
        string? altTransElementsImport,
        string? segmentsImportType,
        string? bilingualTerminologyAction,
        IEnumerable<TagGroupImportPayload>? tagGroups)
    {
        var baseUrl = Creds.Get(CredsNames.Url).Value;
        var url = baseUrl + $"{ApiEndpoints.TMFiles}/import";
        var token = await Client.GetToken(Creds);

        var parameters = new Dictionary<string, object>
        {
            { "customerId", int.Parse(customerId) },
            { "importProjectName", importProjectName },
            { "sourceLanguage", sourceLanguage },
            { "targetLanguage", targetLanguage }
        };

        if (!string.IsNullOrEmpty(tmStatus))
            parameters.Add("tmStatus", tmStatus);
        if (!string.IsNullOrEmpty(tmStatusImportType))
            parameters.Add("tmStatusImportType", tmStatusImportType);
        if (!string.IsNullOrEmpty(whitespacesFormattingType))
            parameters.Add("whitespacesFormattingType", whitespacesFormattingType);
        if (!string.IsNullOrEmpty(altTransElementsImport))
            parameters.Add("altTransElementsImport", altTransElementsImport);
        if (!string.IsNullOrEmpty(segmentsImportType))
            parameters.Add("segmentsImportType", segmentsImportType);
        if (!string.IsNullOrEmpty(bilingualTerminologyAction))
            parameters.Add("bilingualTerminologyAction", bilingualTerminologyAction);
        var xtmRequest = new XTMRequest(new()
        {
            Url = url,
            Method = Method.Post
        }, token);

        foreach (var param in parameters)
            xtmRequest.AddParameter(param.Key, param.Value, ParameterType.GetOrPost);

        AddTagGroupParameters(xtmRequest, tagGroups);

        var fileStream = await _fileManagementClient.DownloadAsync(file);
        var fileBytes = await fileStream.GetByteData();

        xtmRequest.AddFile("file", fileBytes, file.Name);
        xtmRequest.AlwaysMultipartFormData = true;

        try
        {
            var response = await Client.ExecuteXtm<IEnumerable<ImportTMResponse>>(xtmRequest);
            return response.FirstOrDefault()
                   ?? throw new PluginApplicationException("TM import returned no file information.");
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException(ex.Message);
        }
    }

    private List<TagGroupImportPayload> BuildSingleGroupTagPayload(string? tagGroupId, IEnumerable<string>? tagIds)
    {
        if (string.IsNullOrWhiteSpace(tagGroupId) || tagIds == null || !tagIds.Any())
            return [];

        if (!long.TryParse(tagGroupId, out var groupId))
            return [];

        var tags = tagIds
            .Where(x => long.TryParse(x, out _))
            .Select(x => new TagImportPayload { Id = long.Parse(x) })
            .ToList();

        return tags.Count == 0
            ? []
            : [new TagGroupImportPayload { Id = groupId, Tags = tags }];
    }

    private async Task<List<TagGroupImportPayload>> BuildTagPayloadByGroupAsync(IEnumerable<string> tagIds)
    {
        var selectedTagIds = tagIds.ToHashSet(StringComparer.Ordinal);
        var payload = new List<TagGroupImportPayload>();

        var tagGroups = await Client.ExecuteXtmWithJson<List<TagGroupResponse>>(
            ApiEndpoints.TagGroups,
            Method.Get,
            null,
            Creds);

        foreach (var group in tagGroups)
        {
            var groupTags = await Client.ExecuteXtmWithJson<List<TagResponse>>(
                $"{ApiEndpoints.TagGroups}/{group.Id}/tags",
                Method.Get,
                null,
                Creds);

            var matchingTags = groupTags
                .Where(tag => selectedTagIds.Contains(tag.Id) && long.TryParse(tag.Id, out _))
                .Select(tag => new TagImportPayload { Id = long.Parse(tag.Id) })
                .ToList();

            if (!matchingTags.Any() || !long.TryParse(group.Id, out var groupId))
                continue;

            payload.Add(new TagGroupImportPayload
            {
                Id = groupId,
                Tags = matchingTags
            });
        }

        var matchedTagIds = payload
            .SelectMany(x => x.Tags)
            .Select(x => x.Id.ToString())
            .ToHashSet(StringComparer.Ordinal);

        var missingTagIds = selectedTagIds.Where(id => !matchedTagIds.Contains(id)).ToList();
        if (missingTagIds.Any())
            throw new PluginMisconfigurationException(
                $"Could not resolve tag group(s) for tag ID(s): {string.Join(", ", missingTagIds)}.");

        return payload;
    }

    private static void AddTagGroupParameters(
        XTMRequest request,
        IEnumerable<TagGroupImportPayload>? tagGroups)
    {
        if (tagGroups == null)
            return;

        var groupIndex = 0;
        foreach (var group in tagGroups)
        {
            request.AddParameter($"tagGroups[{groupIndex}].id", group.Id, ParameterType.GetOrPost);

            for (var tagIndex = 0; tagIndex < group.Tags.Count; tagIndex++)
            {
                request.AddParameter(
                    $"tagGroups[{groupIndex}].tags[{tagIndex}].id",
                    group.Tags[tagIndex].Id,
                    ParameterType.GetOrPost);
            }

            groupIndex++;
        }
    }

    #endregion

    #region Deprecated XLIFF-based flow

    private async Task<(XDocument doc, string srcLang, string trgLang)> ParseXliffAsync(FileReference fileRef)
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

    #endregion

    #region Common helpers

    private async Task<FileReference> UploadTmxAsync(XDocument tmxDoc, string outputName)
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

        return await UploadTmxAsync(ms.ToArray(), outputName);
    }

    private async Task<FileReference> UploadTmxAsync(byte[] tmxBytes, string outputName)
    {
        using var ms = new MemoryStream(tmxBytes);
        return await _fileManagementClient.UploadAsync(ms, "application/x-tmx+xml", outputName);
    }

    private static string ExtractPlainText(XElement? element)
    {
        if (element == null)
            return string.Empty;

        return string.Concat(element.DescendantNodes().OfType<XText>().Select(t => t.Value));
    }

    private static bool TryExtractPlainTextMessage(byte[] contentBytes, out string message)
    {
        message = string.Empty;
        if (contentBytes.Length == 0)
            return false;

        var preview = Encoding.UTF8.GetString(contentBytes).Trim('\uFEFF', '\u0000', ' ', '\r', '\n', '\t');
        if (string.IsNullOrWhiteSpace(preview))
            return false;

        if (preview.StartsWith("<"))
            return false;

        message = preview;
        return true;
    }

    private static XDocument BuildEmptyTmxDocument(string sourceLanguage)
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
                    new XAttribute("srclang", sourceLanguage),
                    new XAttribute("datatype", "plaintext")),
                new XElement("body")
            )
        );
    }

    #endregion

    private sealed class FilteredTmxResult
    {
        public required byte[] Bytes { get; init; }

        public required int ScannedSegments { get; init; }

        public required int MatchedSegments { get; init; }
    }

    private sealed class TagGroupImportPayload
    {
        [JsonProperty("id")]
        public required long Id { get; init; }

        [JsonProperty("tags")]
        public required List<TagImportPayload> Tags { get; init; }
    }

    private sealed class TagImportPayload
    {
        [JsonProperty("id")]
        public required long Id { get; init; }
    }
}
