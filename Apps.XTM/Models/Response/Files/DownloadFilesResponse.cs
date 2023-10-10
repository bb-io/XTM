namespace Apps.XTM.Models.Response.Files;

public record DownloadFilesResponse<T>(IEnumerable<FileWithData<T>> Files) where T : XtmFileDescription;