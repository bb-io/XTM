using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectDetailedStatusResponse
{
    [JsonProperty("projectId")]
    public long ProjectId { get; set; }

    [JsonProperty("completionStatus")]
    public string CompletionStatus { get; set; }

    [JsonProperty("activity")]
    public string Activity { get; set; }

    [JsonProperty("sourceLanguage")]
    public string SourceLanguage { get; set; }

    [JsonProperty("joinFilesType")]
    public string JoinFilesType { get; set; }

    [JsonProperty("contractorType")]
    public string ContractorType { get; set; }

    [JsonProperty("jobs")]
    public List<ProjectDetailedStatusWorkflowJob> Jobs { get; set; } = [];
}

public class ProjectDetailedStatusWorkflowJob
{
    [JsonProperty("jobId")]
    public string JobId { get; set; }

    [JsonProperty("completionStatus")]
    public string CompletionStatus { get; set; }

    [JsonProperty("fileName")]
    public string FileName { get; set; }

    [JsonProperty("sourceFileId")]
    public long SourceFileId { get; set; }

    [JsonProperty("targetLanguage")]
    public string TargetLanguage { get; set; }

    [JsonProperty("joinFilesType")]
    public string JoinFilesType { get; set; }

    [JsonProperty("steps")]
    public List<ProjectDetailedStatusWorkflowStep> Steps { get; set; } = [];
}

public class ProjectDetailedStatusWorkflowStep
{
    private string? _referenceStepName;
    private string? _legacyStepReferenceName;

    [JsonProperty("workflowStepName")]
    public string WorkflowStepName { get; set; }

    [JsonProperty("displayStepName")]
    public string DisplayStepName { get; set; }

    [JsonProperty("referenceStepName")]
    public string StepReferenceName
    {
        get => _referenceStepName ?? _legacyStepReferenceName ?? string.Empty;
        set => _referenceStepName = value;
    }

    [JsonProperty("stepReferenceName")]
    private string? LegacyStepReferenceName
    {
        set => _legacyStepReferenceName = value;
    }

    [JsonProperty("status")]
    public string Status { get; set; }
}
