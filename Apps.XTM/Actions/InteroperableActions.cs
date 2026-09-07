using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using Apps.XTM.Models.Response.Projects;
using Apps.XTM.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Models;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff2;
using RestSharp;

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

        var fileName = input.Name?.Trim() ?? input.File.Name ??
            throw new PluginMisconfigurationException("File name is required");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new PluginMisconfigurationException("Provide a name for the source file.");

        await using var fileStream = await fileManagementClient.DownloadAsync(input.File);
        var sourceBytes = await fileStream.GetByteData();

        var projectDetails = await Client.ExecuteXtmWithJson<FullProject>(
            $"{ApiEndpoints.Projects}/{project.ProjectId}", Method.Get, null, Creds);

        var prepared = XliffSourceSelection.Prepare(
            sourceBytes,
            input.ExcludeSegmentStates,
            input.File.Name ?? fileName,
            input.File.ContentType,
            projectDetails.SourceLanguage);

        if (!new[] { ".xlf", ".xliff" }.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            fileName += ".xlf";

        await using var preparedStream = new MemoryStream(prepared.Content);
        var preparedFile = await fileManagementClient.UploadAsync(
            preparedStream,
            "application/xliff+xml",
            fileName);

        CreateProjectResponse? uploadResponse = null;
        if (prepared.SegmentsLeft > 0)
            uploadResponse = await _fileActions.UploadSourceFileBytes(project, input, prepared.Content, fileName);

        return new UploadSelectedSourceXliffResponse
        {
            Name = uploadResponse?.Name ?? fileName,
            ProjectId = uploadResponse?.ProjectId ?? project.ProjectId,
            Jobs = uploadResponse?.Jobs?.Where(x => x.FileName == fileName).ToArray() ?? [],
            File = preparedFile,
            Uploaded = uploadResponse != null,
            SegmentsExcluded = prepared.SegmentsExcluded,
            SegmentsTotal = prepared.SegmentsTotal,
            SegmentsLeft = prepared.SegmentsLeft,
            ApproximateWordCount = prepared.ApproximateWordCount,
        };
    }

    [Action("Download translated interoperable file", Description = "Generate and download a translated file, restoring segments excluded by Upload interoperable source file")]
    public async Task<FileResponse> DownloadTranslatedInteroperableFile(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DownloadTranslatedInteroperableFileRequest input)
    {
        if (string.IsNullOrWhiteSpace(project.ProjectId) || string.IsNullOrWhiteSpace(input.JobId))
            throw new PluginMisconfigurationException("Provide the project ID and job ID from Upload interoperable source file.");

        var generated = await _fileActions.GenerateFiles(project, new GenerateFileRequest
        {
            FileType = "TARGET",
            JobIds = [input.JobId],
        });

        GeneratedFileResponse? file;
        try
        {
            file = generated.Files.SingleOrDefault(x => x.JobId == input.JobId && x.FileType == "TARGET");
        }
        catch (InvalidOperationException)
        {
            throw new PluginApplicationException("XTM returned multiple translated files for the selected job. Expected one TARGET file.");
        }

        if (file is null || string.IsNullOrWhiteSpace(file.FileId))
            throw new PluginApplicationException("XTM did not generate a translated file for the selected job. Check that the job has finished analysis and try again.");

        var fileUrl = $"{ApiEndpoints.Projects}/{project.ProjectId}/files/{file.FileId}";
        var finished = false;

        // retry for 6-7 mins
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var status = await Client.ExecuteXtmWithJson<GeneratedFileStatusResponse>(
                $"{fileUrl}/status?fileScope=JOB", Method.Get, null, Creds);

            if (status.Status == "FINISHED")
            {
                finished = true;
                break;
            }

            if (status.Status is "ERROR" or "WARNING")
                throw new PluginApplicationException($"XTM could not generate the translated file: {status.Status}. {status.Message}");

            await Task.Delay(5000);
        }

        if (!finished)
            throw new PluginApplicationException("XTM is still generating the translated file. Try this action again shortly.");

        var response = await Client.ExecuteXtmWithJson(
            $"{fileUrl}/download?fileScope=JOB", Method.Get, null, Creds);

        if (response.RawBytes is not { Length: > 0 })
            throw new PluginApplicationException("XTM returned an empty translated file. Generate the file again and retry.");

        using var archive = new MemoryStream(response.RawBytes);

        IEnumerable<BlackbirdZipEntry> entries;
        try
        {
            entries = await archive.GetFilesFromZip();
        }
        catch (Exception exception)
        {
            throw new PluginApplicationException($"XTM returned an invalid translated file archive. {exception.Message}");
        }

        var files = entries.ToArray();

        if (files.Length != 1)
            throw new PluginApplicationException($"Expected one translated file for the selected job, but XTM returned {files.Length}.");

        await using var targetStream = files[0].FileStream;
        var targetBytes = await targetStream.GetByteData();
        using var xliffStream = new MemoryStream(targetBytes);

        if (!Xliff2Serializer.IsXliff2(xliffStream, out _))
            throw new PluginMisconfigurationException("This job does not contain an interoperable XLIFF source. Select a job created by Upload interoperable source file.");

        var restored = XliffSourceSelection.RemoveBlackbirdExclusions(targetBytes);
        await using var restoredStream = new MemoryStream(restored);

        return new(await fileManagementClient.UploadAsync(
            restoredStream, "application/xliff+xml", files[0].UploadName));
    }
}
