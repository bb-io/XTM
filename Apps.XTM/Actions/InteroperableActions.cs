using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Models.Response.Workflows;
using Apps.XTM.Utils;
using Apps.XTM.Webhooks.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Models;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;
using RestSharp;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Apps.XTM.Actions;

[ActionList("Interoperable")]
public class InteroperableActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : XtmInvocable(invocationContext)
{
    private readonly FileActions _fileActions = new(invocationContext, fileManagementClient);

    [Action("Upload interoperable source file", Description = "Convert a file to XLIFF 2.1 and upload it as a source file, excluding segments in selected states from translation")]
    public async Task<UploadSelectedSourceXliffResponse> UploadSelectedSourceXliff(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] UploadSelectedSourceXliffRequest input)
    {
        if (input.File is null || string.IsNullOrWhiteSpace(project.ProjectId))
            throw new PluginMisconfigurationException("Provide a source file and project ID.");

        var fileName = input.Name?.Trim() ?? input.File.Name?.Trim();

        if (string.IsNullOrWhiteSpace(fileName))
            throw new PluginMisconfigurationException("Provide a name for the source file.");

        var sourceName = fileName;
        if (!new[] { ".xlf", ".xliff" }.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            fileName += ".xlf";

        await using var fileStream = await fileManagementClient.DownloadAsync(input.File);
        var sourceBytes = await fileStream.GetByteData();

        var projectDetails = await Client.ExecuteXtmWithJson<FullProject>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}", Method.Get, null, Creds);

        var prepared = XliffSourceSelection.Prepare(
            sourceBytes,
            input.ExcludeSegmentStates,
            input.File.Name ?? sourceName,
            input.File.ContentType,
            projectDetails.SourceLanguage);

        await using var stream = new MemoryStream(prepared.Content);
        var preparedFile = await fileManagementClient.UploadAsync(stream, "application/xliff+xml", fileName);

        var upload = prepared.SegmentsLeft > 0
            ? await _fileActions.UploadSourceFileBytes(project, input, prepared.Content, fileName)
            : null;

        return new UploadSelectedSourceXliffResponse
        {
            Name = upload?.Name ?? fileName,
            ProjectId = upload?.ProjectId ?? project.ProjectId,
            Jobs = upload?.Jobs?.Where(x => x.FileName == fileName).ToArray() ?? [],
            File = preparedFile,
            Uploaded = upload != null,
            SegmentsExcluded = prepared.SegmentsExcluded,
            SegmentsTotal = prepared.SegmentsTotal,
            SegmentsLeft = prepared.SegmentsLeft,
            ApproximateWordCount = prepared.ApproximateWordCount,
        };
    }

    [Action("Download translated interoperable file", Description = "Generate and download a translated file with unit provenance from XTM's offline XLIFF, restoring segments excluded by Upload interoperable source file")]
    public async Task<FileResponse> DownloadTranslatedInteroperableFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadTranslatedInteroperableFileRequest input)
    {
        if (string.IsNullOrWhiteSpace(project.ProjectId) || string.IsNullOrWhiteSpace(input.JobId))
            throw new PluginMisconfigurationException("Provide the project ID and job ID from Upload interoperable source file.");

        var attributionMode = string.IsNullOrWhiteSpace(input.AttributeSegmentsToUser)
            ? SegmentAttributionDataSourceHandler.OnlyConfirmed
            : input.AttributeSegmentsToUser.Trim().ToLowerInvariant();

        if (attributionMode is not (
            SegmentAttributionDataSourceHandler.All
            or SegmentAttributionDataSourceHandler.OnlyConfirmed
            or SegmentAttributionDataSourceHandler.OnlyChanged
            or SegmentAttributionDataSourceHandler.None))
        {
            throw new PluginMisconfigurationException(
                "Attribute segments to user must be all segments, only confirmed segments, only changed segments, or no segments.");
        }

        var provenanceTypeOverride = string.IsNullOrWhiteSpace(input.ProvenanceType)
            ? null
            : input.ProvenanceType.Trim().ToLowerInvariant();

        if (provenanceTypeOverride is not null
            and not ProvenanceTypeDataSourceHandler.Translation
            and not ProvenanceTypeDataSourceHandler.Review)
        {
            throw new PluginMisconfigurationException("Provenance type must be translation or review.");
        }

        var generatedFiles = await GenerateJobFiles(project, input.JobId);

        var (provenanceType, assignments) = await ResolveWorkflowAttribution(
            project.ProjectId, input.JobId, input.WorkflowStep, attributionMode, provenanceTypeOverride);

        await WaitForGeneratedFiles(project.ProjectId, generatedFiles);

        var downloadedFiles = await DownloadGeneratedFiles(project.ProjectId, generatedFiles);

        var target = downloadedFiles["TARGET"].Bytes;
        byte[] withProvenance;
        try
        {
            withProvenance = ApplyProvenance(target, downloadedFiles["XLIFF"].Bytes,
                attributionMode, provenanceType, assignments);
        }
        catch (PluginApplicationException exception)
        {
            InvocationContext.Logger?.LogWarning(
                $"[XTM_DownloadTranslatedInteroperableFile] Provenance mapping failed for project {project.ProjectId}, job {input.JobId}: {exception.Message}", []);
            throw;
        }

        var restored = XliffSourceSelection.RemoveBlackbirdExclusions(withProvenance);

        await using var stream = new MemoryStream(restored);
        var fileReference = await fileManagementClient.UploadAsync(stream, "application/xliff+xml", downloadedFiles["TARGET"].Name);

        return new(fileReference);
    }

    private async Task<Dictionary<string, GeneratedFileResponse>> GenerateJobFiles(ProjectRequest project, string jobId)
    {
        // Start both generations before polling so XTM can prepare them concurrently.
        var generations = await Task.WhenAll(new[] { "TARGET", "XLIFF" }.Select(fileType =>
            _fileActions.GenerateFiles(project, new GenerateFileRequest
            {
                FileType = fileType,
                JobIds = [jobId],
            })));
        var generatedFiles = generations.SelectMany(x => x.Files).ToArray();
        var selectedFiles = new Dictionary<string, GeneratedFileResponse>();
        foreach (var fileType in new[] { "TARGET", "XLIFF" })
        {
            var matches = generatedFiles.Where(x => x.JobId == jobId && x.FileType == fileType).ToArray();
            if (matches.Length != 1 || string.IsNullOrWhiteSpace(matches[0].FileId))
                throw new PluginApplicationException($"Expected one generated {fileType} file for the selected job, but XTM returned {matches.Length}. Check that the job has finished analysis and try again.");
            selectedFiles.Add(fileType, matches[0]);
        }
        return selectedFiles;
    }

    private async Task<(string ProvenanceType, List<WorkflowAssignmentBundleResponse> Assignments)> ResolveWorkflowAttribution(
        string projectId, string jobId, string? workflowStep, string attributionMode, string? provenanceOverride)
    {
        var provenanceType = provenanceOverride ?? ProvenanceTypeDataSourceHandler.Translation;
        if (attributionMode == SegmentAttributionDataSourceHandler.None && provenanceOverride is not null)
            return (provenanceType, []);

        var (manualSteps, definitions) = await GetManualWorkflowSteps(projectId, jobId);

        var selectedStep = await SelectAttributionStep(projectId, jobId, workflowStep, manualSteps);
        if (selectedStep is null)
            return (provenanceType, []);

        var definition = definitions.FirstOrDefault(x =>
            string.Equals(x.Id, selectedStep.Id, StringComparison.OrdinalIgnoreCase));
        provenanceType = provenanceOverride ?? (definition?.Role?.Trim().ToUpperInvariant() switch
        {
            "REVIEW" or "CORRECT" or "LQA" => ProvenanceTypeDataSourceHandler.Review,
            _ => ProvenanceTypeDataSourceHandler.Translation,
        });
        var assignments = attributionMode == SegmentAttributionDataSourceHandler.None
            ? []
            : await GetStepAssignments(projectId, jobId, selectedStep);
        return (provenanceType, assignments);
    }

    private async Task<(List<ProjectWorkflowStepResponse> Steps, List<WorkflowStepResponse> Definitions)> GetManualWorkflowSteps(
        string projectId, string jobId)
    {
        var workflows = await Client.ExecuteXtmWithJson<List<ProjectWorkflowResponse>>(
            $"{ApiEndpoints.Projects}/{projectId}/workflow?jobIds={jobId}", Method.Get, null, Creds);
        var projectSteps = workflows.SelectMany(x => x.Steps).ToList();
        var stepIds = projectSteps.Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
        var definitions = stepIds.Length == 0
            ? []
            : await Client.ExecuteXtmWithJson<List<WorkflowStepResponse>>(
                $"{ApiEndpoints.Workflows}{ApiEndpoints.Steps}?ids={string.Join("&ids=", stepIds)}",
                Method.Get, null, Creds);
        var manualStepIds = definitions
            .Where(x => x.Type?.Trim().ToUpperInvariant() is "ONLINE_TRANSLATION"
                or "OFFLINE_PROCESSING_READ_ONLY" or "OFFLINE_PROCESSING" or "EXTERNAL_MANUAL" or "OUTER_MANUAL")
            .Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (projectSteps.Where(x => manualStepIds.Contains(x.Id)).ToList(), definitions);
    }

    private async Task<ProjectWorkflowStepResponse?> SelectAttributionStep(
        string projectId, string jobId, string? workflowStep, List<ProjectWorkflowStepResponse> manualSteps)
    {
        if (!string.IsNullOrWhiteSpace(workflowStep))
        {
            var matches = manualSteps.Where(x => StepNamesMatch(x.ReferenceStepName, workflowStep)).ToArray();
            if (matches.Length == 0)
                matches = manualSteps.Where(x => StepNamesMatch(x.DisplayStepName, workflowStep)).ToArray();
            if (matches.Length == 0)
                matches = manualSteps.Where(x => StepNamesMatch(x.Name, workflowStep)).ToArray();
            if (matches.Length == 0)
                throw new PluginMisconfigurationException(
                    $"Workflow step '{workflowStep}' does not exist in this job or is not a recognized manual step.");
            if (matches.Length > 1)
                throw new PluginMisconfigurationException(
                    $"Workflow step '{workflowStep}' matches multiple steps. Select a unique workflow step reference name.");
            return matches[0];
        }

        if (manualSteps.Count == 0)
            return null;

        var projectStatus = await Client.ExecuteXtmWithJson<ProjectDetailedStatusResponse>(
            $"{ApiEndpoints.Projects}/{projectId}/status?fetchLevel=STEPS", Method.Get, null, Creds);

        var jobSteps = projectStatus.Jobs.FirstOrDefault(x => x.JobId == jobId)?.Steps ?? [];
        var stepsWithStatus = manualSteps.Select(step =>
        {
            var matches = jobSteps.Where(status =>
                !string.IsNullOrWhiteSpace(status.StepReferenceName) && !string.IsNullOrWhiteSpace(step.ReferenceStepName)
                    ? StepNamesMatch(status.StepReferenceName, step.ReferenceStepName)
                    : !string.IsNullOrWhiteSpace(status.DisplayStepName) && !string.IsNullOrWhiteSpace(step.DisplayStepName)
                        ? StepNamesMatch(status.DisplayStepName, step.DisplayStepName)
                        : StepNamesMatch(status.WorkflowStepName, step.Name)).ToArray();
            return (Step: step, Status: matches.Length == 1 ? matches[0].Status : null);
        }).ToArray();
        return stepsWithStatus.LastOrDefault(x =>
                string.Equals(x.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase)).Step
            ?? stepsWithStatus.LastOrDefault(x =>
                string.Equals(x.Status, "FINISHED", StringComparison.OrdinalIgnoreCase)).Step;
    }

    private async Task<List<WorkflowAssignmentBundleResponse>> GetStepAssignments(
        string projectId, string jobId, ProjectWorkflowStepResponse selectedStep)
    {
        var assignments = await Client.ExecuteXtmWithJson<WorkflowAssignmentsResponse>(
            $"{ApiEndpoints.Projects}/{projectId}/workflow/assignment?jobIds={jobId}", Method.Get, null, Creds);
        var steps = assignments.Jobs.FirstOrDefault(x => x.JobId == jobId)?.Steps ?? [];
        var matches = steps.Where(step =>
            !string.IsNullOrWhiteSpace(step.ReferenceStepName) && !string.IsNullOrWhiteSpace(selectedStep.ReferenceStepName)
                ? StepNamesMatch(step.ReferenceStepName, selectedStep.ReferenceStepName)
                : !string.IsNullOrWhiteSpace(step.DisplayStepName) && !string.IsNullOrWhiteSpace(selectedStep.DisplayStepName)
                    ? StepNamesMatch(step.DisplayStepName, selectedStep.DisplayStepName)
                    : StepNamesMatch(step.Name, selectedStep.Name)).ToArray();
        if (matches.Length > 1)
            throw new PluginApplicationException("XTM returned multiple assignments for the selected workflow step. Select a unique workflow step reference name.");
        return matches.SingleOrDefault()?.Bundles ?? [];
    }

    private static bool StepNamesMatch(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private async Task WaitForGeneratedFiles(string projectId, IReadOnlyDictionary<string, GeneratedFileResponse> selectedFiles)
    {
        var pending = selectedFiles.ToDictionary(x => x.Key, x => x.Value);
        // retry for 6-7 mins
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var statuses = await Task.WhenAll(pending.Select(async entry =>
            {
                var status = await Client.ExecuteXtmWithJson<GeneratedFileStatusResponse>(
                    $"{ApiEndpoints.Projects}/{projectId}/files/{entry.Value.FileId}/status?fileScope=JOB",
                    Method.Get, null, Creds);
                return (FileType: entry.Key, Status: status);
            }));

            foreach (var (fileType, status) in statuses)
            {
                if (status.Status is "ERROR" or "WARNING")
                    throw new PluginApplicationException($"XTM could not generate the {fileType} file: {status.Status}. {status.Message}");
                if (status.Status == "FINISHED")
                    pending.Remove(fileType);
            }

            if (pending.Count == 0)
                break;
            if (attempt < 79)
                await Task.Delay(5000);
        }

        if (pending.Count != 0)
            throw new PluginApplicationException($"XTM is still generating {string.Join(" and ", pending.Keys)}. Try this action again shortly.");
    }

    private async Task<Dictionary<string, (byte[] Bytes, string Name)>> DownloadGeneratedFiles(
        string projectId, IReadOnlyDictionary<string, GeneratedFileResponse> selectedFiles)
    {
        var downloads = await Task.WhenAll(selectedFiles.Select(async entry =>
        {
            var response = await Client.ExecuteXtmWithJson(
                $"{ApiEndpoints.Projects}/{projectId}/files/{entry.Value.FileId}/download?fileScope=JOB",
                Method.Get, null, Creds);
            return (FileType: entry.Key, Bytes: response.RawBytes);
        }));
        var content = new Dictionary<string, (byte[] Bytes, string Name)>();
        foreach (var (fileType, bytes) in downloads)
        {
            if (bytes is not { Length: > 0 })
                throw new PluginApplicationException($"XTM returned an empty {fileType} file. Generate the file again and retry.");
            using var archive = new MemoryStream(bytes);
            BlackbirdZipEntry[] entries;
            try
            {
                entries = (await archive.GetFilesFromZip()).ToArray();
            }
            catch (Exception exception)
            {
                throw new PluginApplicationException($"XTM returned an invalid {fileType} archive. {exception.Message}");
            }
            try
            {
                if (entries.Length != 1)
                    throw new PluginApplicationException($"Expected one {fileType} file for the selected job, but XTM returned {entries.Length}.");
                content.Add(fileType, (await entries[0].FileStream.GetByteData(), entries[0].UploadName));
            }
            finally
            {
                foreach (var entry in entries)
                    await entry.FileStream.DisposeAsync();
            }
        }

        return content;
    }

    private static byte[] ApplyProvenance(byte[] translated, byte[] offline, string attributionMode,
        string provenanceType, IReadOnlyList<WorkflowAssignmentBundleResponse> assignedBundles)
    {
        // XTM replaces original IDs with t1, t2, ... in offline XLIFF. Flatten the TARGET's
        // translatable segments in document order, excluding non-translatable units, ignorables, and
        // blank text-only sources, then pair by position with offline trans-units. Validate counts,
        // source/target content, and inline-code topology before updating the original parent units.
        // This relies on XTM preserving order, as observed in live probes: reordered segments with
        // identical source AND target content cannot be detected. This is not an identity-based join.
        // Offline tN IDs are used only for workflow assignment ranges, not original unit matching.
        using var targetStream = new MemoryStream(translated);
        var targetLoad = Transformation.Load(targetStream, "translated.xlf");
        if (!targetLoad.Success || !targetLoad.WasBilingual)
            throw new PluginApplicationException($"XTM returned invalid XLIFF for the translated file. {targetLoad.Error}");

        using var offlineStream = new MemoryStream(offline);
        var offlineLoad = Transformation.Load(offlineStream, "offline.xlf");
        if (!offlineLoad.Success || !offlineLoad.WasBilingual)
            throw new PluginApplicationException($"XTM returned invalid XLIFF for the offline file. {offlineLoad.Error}");

        // Load results do not expose the input XLIFF version; use Filters' format recognizers.
        if (!Xliff2Serializer.IsXliff2(targetStream, out _) || !Xliff1Serializer.IsXliff1(offlineStream, out _))
            throw new PluginApplicationException("Provenance requires the translated XLIFF 2 file and XTM's offline XLIFF 1.2 file.");

        // Filters' model cannot retain original node order, all whitespace, or exact native suggestions.
        // Keep XML through its parser only for those preservation and attribution gaps.
        var targetDocument = targetStream.ParseXmlWithBomFallback(LoadOptions.PreserveWhitespace)!;
        var offlineRoot = offlineStream.ParseXmlWithBomFallback(LoadOptions.PreserveWhitespace)!.Root!;
        var xliff = targetDocument.Root!.Name.Namespace;
        var xtm = Xliff1Serializer.XliffNs;

        // Filters 1.2.17 can report success with no units for prefix-only XLIFF. Normalize its
        // default namespace declaration and reload; this is a reader workaround, not format validation.
        if (offlineRoot.GetDefaultNamespace() != offlineRoot.Name.Namespace)
        {
            offlineRoot.SetAttributeValue("xmlns", offlineRoot.Name.NamespaceName);
            using var normalizedStream = new MemoryStream(Encoding.UTF8.GetBytes(offlineRoot.ToString(SaveOptions.DisableFormatting)));
            offlineLoad = Transformation.Load(normalizedStream, "offline.xlf");
            if (!offlineLoad.Success || !offlineLoad.WasBilingual)
                throw new PluginApplicationException($"XTM returned invalid XLIFF for the offline file. {offlineLoad.Error}");
        }

        var offlineTransformation = offlineLoad.Value;
        var offlineFiles = offlineTransformation.Children.OfType<Transformation>().ToArray();
        if (offlineFiles.Length == 0)
            offlineFiles = [offlineTransformation];

        // XTM replaces source IDs with t1, t2, ... and omits excluded units and ignorables.
        // Both exports belong to the same generated job; validate their ordered content before attribution.
        var segments = targetDocument.Descendants(xliff + "unit")
            .Where(unit => unit.AncestorsAndSelf().Attributes("translate").FirstOrDefault()?.Value != "no")
            .SelectMany(unit => unit.Elements(xliff + "segment"))
            .Where(segment => segment.Element(xliff + "source") is { } source
                && (!string.IsNullOrWhiteSpace(source.Value) || source.HasElements))
            .ToArray();
        var offlineSegments = offlineFiles.SelectMany(file => file.GetUnits().Select(unit => (File: file, Unit: unit))).ToArray();
        var originalOfflineUnits = offlineRoot.Descendants(xtm + "trans-unit").ToArray();
        if (offlineSegments.Length != originalOfflineUnits.Length)
            throw new PluginApplicationException("XTM's offline XLIFF contains unsupported unit structure. Provenance cannot be mapped safely.");
        if (segments.Length != offlineSegments.Length)
            throw new PluginApplicationException($"The translated XLIFF contains {segments.Length} translatable segments, but XTM's offline XLIFF contains {offlineSegments.Length}. Provenance cannot be mapped safely.");

        var unitEvidence = new Dictionary<XElement, List<(string? Person, string? Qualifier)>>();
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var exported = offlineSegments[index];
            var originalExport = originalOfflineUnits[index];
            // A segmented XLIFF 1 unit has no single model segment representing its wrapper source/target.
            var offlineSegment = originalExport.Element(xtm + "seg-source") is null
                ? exported.Unit.Segments.Single() : null;
            var source = segment.Element(xliff + "source")!;
            var offlineSource = originalExport.Element(xtm + "source");
            var target = segment.Element(xliff + "target");
            var offlineTarget = originalExport.Element(xtm + "target");
            var sourceContent = NormalizeElementContent(source);
            var targetContent = NormalizeElementContent(target);
            var offlineContent = NormalizeElementContent(offlineTarget, parts: offlineSegment?.Target);
            var copiedSource = offlineContent.Length == 0 && targetContent == sourceContent
                && exported.File.Other.OfType<XAttribute>().FirstOrDefault(attribute =>
                    attribute.Name == XNamespace.Get("urn:xliff-xtm-extensions") + "populate-target-with-source")?.Value == "yes";
            if (offlineSource is null
                || sourceContent != NormalizeElementContent(offlineSource, parts: offlineSegment?.Source)
                || (targetContent != offlineContent && !copiedSource))
                throw new PluginApplicationException($"Segment {index + 1} differs between the translated and offline XLIFF. Provenance cannot be mapped safely; finish editing and generate both files again.");

            var qualifier = (offlineSegment is null
                ? (string?)offlineTarget?.Attribute("state-qualifier")
                : offlineSegment.TargetAttributes.FirstOrDefault(attribute => attribute.Name == "state-qualifier")?.Value)
                ?.Trim().ToLowerInvariant();
            // Filters normalizes whitespace in Unit.Other. Keep native suggestion XML for exact change evidence.
            var alternatives = originalExport.Elements(xtm + "alt-trans")
                .Where(alternative => string.Equals((string?)alternative.Attribute("extype"),
                    qualifier == "mt-suggestion" ? "MACHINE-TRANSLATION" : qualifier,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var baseline = alternatives.Length == 1 && !string.IsNullOrWhiteSpace(qualifier)
                ? alternatives[0].Element(xtm + "target")
                : null;
            // alt-trans is a suggestion, not edit history. Without a known baseline, change is unknown.
            var changed = baseline is not null
                && NormalizeElementContent(offlineTarget, preserveInlineContent: true)
                != NormalizeElementContent(baseline, preserveInlineContent: true);
            var attributePerson = attributionMode switch
            {
                SegmentAttributionDataSourceHandler.All => true,
                SegmentAttributionDataSourceHandler.OnlyConfirmed =>
                    offlineSegment?.State == SegmentState.Reviewed
                    || (offlineSegment?.State is null && string.Equals((string?)offlineTarget?.Attribute("state"),
                        "signed-off", StringComparison.OrdinalIgnoreCase)),
                SegmentAttributionDataSourceHandler.OnlyChanged => changed,
                _ => false,
            };
            var position = int.TryParse(exported.Unit.Id?.TrimStart('t'), out var number)
                ? number : index + 1;
            var people = attributePerson
                ? assignedBundles.Where(bundle => (!bundle.From.HasValue || position >= bundle.From)
                    && (!bundle.To.HasValue || position <= bundle.To)
                    && !string.IsNullOrWhiteSpace(bundle.UserId) && !string.IsNullOrWhiteSpace(bundle.UserName))
                    .Select(bundle => $"{bundle.UserName} (ID {bundle.UserId})").Distinct().ToArray()
                : [];
            if (people.Length > 1)
                throw new PluginApplicationException($"XTM returned overlapping assignees for segment {position}. Select an unambiguous workflow assignment before attributing provenance.");
            var unit = segment.Parent!;
            if (!unitEvidence.TryGetValue(unit, out var evidence))
                unitEvidence.Add(unit, evidence = []);
            evidence.Add((people.SingleOrDefault(), qualifier));
        }

        // Serialize only provenance through Filters: a full XLIFF roundtrip can normalize
        // segment content and reorder groups. Keep the original document for the returned file.
        var provenanceTransformation = new Transformation(null);
        var isReview = provenanceType == ProvenanceTypeDataSourceHandler.Review;
        foreach (var evidence in unitEvidence.Values)
        {
            var people = evidence.Select(x => x.Person).Where(x => x is not null).Distinct().ToArray();
            var qualifiers = evidence.Select(x => x.Qualifier).Distinct().ToArray();
            // A unit-wide person claim is only valid if all its segments qualify for the same assignee.
            var person = people.Length == 1 && evidence.All(x => x.Person == people[0]) ? people[0] : null;
            var tool = person is not null || qualifiers.Length != 1 || string.IsNullOrWhiteSpace(qualifiers[0])
                ? "XTM"
                : $"XTM ({Regex.Replace(qualifiers[0]!.Trim().ToLowerInvariant(), "[-_]+", " ")})";
            var unit = new Unit(new Xliff2Coder());
            var provenance = isReview ? unit.Provenance.Review : unit.Provenance.Translation;
            provenance.Person = person;
            provenance.Tool = tool;
            provenanceTransformation.Children.Add(unit);
        }

        var serializedUnits = Xliff2Serializer.Serialize(provenanceTransformation, Xliff2Version.Xliff21)
            .ParseXmlWithBomFallback()!.Descendants().Where(element => element.Name.LocalName == "unit");
        var replacedAttributes = (isReview
                ? new[] { "revPerson", "revPersonRef", "revTool", "revToolRef" }
                : new[] { "person", "personRef", "tool", "toolRef" })
            .Select(name => Xliff2Serializer.ItsNs + name).ToArray();
        // Pair by insertion order, since original unit IDs can repeat across files.
        // Clear stale references without changing organizations or the other provenance role.
        foreach (var (originalUnit, serializedUnit) in unitEvidence.Keys.Zip(serializedUnits))
        {
            originalUnit.Attributes().Where(attribute => replacedAttributes.Contains(attribute.Name)).Remove();
            originalUnit.Add(serializedUnit.Attributes().Where(attribute => attribute.Name.Namespace == Xliff2Serializer.ItsNs));
        }

        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = targetDocument.Declaration is null,
        }))
            targetDocument.Save(writer);
        return output.ToArray();
    }

    private static string NormalizeElementContent(XElement? element, bool trim = true, bool preserveInlineContent = false,
        IEnumerable<LineElement>? parts = null)
    {
        if (element is null)
            return string.Empty;

        // XLIFF 1 stores native code inside bpt/ept/ph; XLIFF 2 uses pc/ph/sc/ec.
        // Cross-format matching compares text and code topology without XTM's regenerated inline IDs.
        var content = new StringBuilder();
        // Load can drop whitespace-only text between codes. Unsupported XML and segmented wrappers
        // also need original nodes; native suggestion comparisons must retain code payloads.
        if (parts is not null && !preserveInlineContent
            && !element.DescendantNodes().OfType<XText>().Any(text => string.IsNullOrWhiteSpace(text.Value))
            && element.Descendants().All(child =>
                child.Name.Namespace == Xliff1Serializer.XliffNs
                && (child.Name.LocalName is "g" or "bpt" or "bx" or "ept" or "ex" or "ph" or "x"
                    || child.Name.LocalName == "mrk" && (string?)child.Attribute("mtype") != "seg"
                        && child.Attribute(XNamespace.Get("http://blackbird.io/") + "position") is null)))
        {
            foreach (var part in parts)
                content.Append(part switch
                {
                    StartTag => "\uE000",
                    EndTag => "\uE001",
                    InlineTag => "\uE002",
                    AnnotationStart or AnnotationEnd => string.Empty,
                    _ => part.Value,
                });
        }
        else
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText text)
                    content.Append(text.Value);
                else if (node is XElement child)
                {
                    if (preserveInlineContent)
                    {
                        // Both targets are offline XLIFF: retain native code content and order.
                        // Native payload identifies a code even when XTM regenerates its numeric IDs.
                        // Empty codes have no payload, so retain their IDs to detect substitutions.
                        var inline = new XElement(child);
                        foreach (var code in inline.DescendantsAndSelf())
                        {
                            if (code.Name.LocalName is "ph" or "bpt" or "ept" or "it" && code.Value.Length > 0)
                                code.Attributes().Where(x => x.Name == "id" || x.Name == "rid").Remove();
                            code.ReplaceAttributes(code.Attributes().OrderBy(x => x.Name.ToString(), StringComparer.Ordinal).ToArray());
                            // Treat self-closing and explicit empty tags as the same content.
                            if (!code.Nodes().Any())
                                code.RemoveNodes();
                        }
                        content.Append(inline.ToString(SaveOptions.DisableFormatting));
                        continue;
                    }

                    switch (child.Name.LocalName)
                    {
                        case "bpt": case "sc": case "bx":
                            content.Append('\uE000');
                            break;
                        case "ept": case "ec": case "ex":
                            content.Append('\uE001');
                            break;
                        case "ph": case "x":
                            content.Append('\uE002');
                            break;
                        case "pc": case "g":
                            content.Append('\uE000').Append(NormalizeElementContent(child, false)).Append('\uE001');
                            break;
                        default:
                            content.Append(NormalizeElementContent(child, false));
                            break;
                    }
                }
            }
        }
        var value = content.ToString();
        return !trim || element.AncestorsAndSelf().Attributes(XNamespace.Xml + "space").FirstOrDefault()?.Value == "preserve"
            ? value : value.Trim();
    }
}
