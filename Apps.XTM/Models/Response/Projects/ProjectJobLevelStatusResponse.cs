using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectJobLevelStatusDto
{
    [Display("Project ID")]
    [JsonProperty("projectId")]
    public int ProjectId { get; set; }

    [Display("Completion status")]
    [JsonProperty("completionStatus")]
    public string CompletionStatus { get; set; } = string.Empty;

    [JsonProperty("activity")]
    public string Activity { get; set; } = string.Empty;

    [Display("Source language")]
    [JsonProperty("sourceLanguage")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Join files type")]
    [JsonProperty("joinFilesType")]
    public string JoinFilesType { get; set; } = string.Empty;

    [JsonProperty("jobs")]
    public IEnumerable<JobDto> Jobs { get; set; } = [];
}

public class JobDto
{
    [Display("Job ID")]
    [JsonProperty("jobId")]
    public int JobId { get; set; }

    [Display("Completion status")]
    [JsonProperty("completionStatus")]
    public string CompletionStatus { get; set; } = string.Empty;

    [Display("File name")]
    [JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;

    [Display("Source file ID")]
    [JsonProperty("sourceFileId")]
    public int SourceFileId { get; set; }

    [Display("Target language")]
    [JsonProperty("targetLanguage")]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Join files type")]
    [JsonProperty("joinFilesType")]
    public string JoinFilesType { get; set; } = string.Empty;
}