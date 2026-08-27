using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.Provenance;

public class ProjectStatisticsResponse
{
    [JsonProperty("targetLanguage")]
    public string TargetLanguage { get; set; } = string.Empty;

    [JsonProperty("usersStatistics")]
    public List<UserStatisticsResponse> UsersStatistics { get; set; } = [];
}

public class UserStatisticsResponse
{
    [JsonProperty("userDisplayName")]
    public string? UserDisplayName { get; set; }

    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("stepsStatistics")]
    public List<StepStatisticsResponse> StepsStatistics { get; set; } = [];
}

public class StepStatisticsResponse
{
    [JsonProperty("workflowStepName")]
    public string WorkflowStepName { get; set; } = string.Empty;

    [JsonProperty("stepReferenceName")]
    public string StepReferenceName { get; set; } = string.Empty;

    [JsonProperty("referenceStepName")]
    public string ReferenceStepName { get; set; } = string.Empty;

    [JsonProperty("jobsStatistics")]
    public List<JobStatisticsResponse> JobsStatistics { get; set; } = [];
}

public class JobStatisticsResponse
{
    [JsonProperty("jobId")]
    public long JobId { get; set; }

    [JsonProperty("lastCompletionDate")]
    public long LastCompletionDate { get; set; }
}

public class JobMetricsResponse
{
    [JsonProperty("jobId")]
    public long JobId { get; set; }

    [JsonProperty("coreMetrics")]
    public CoreMetricsResponse CoreMetrics { get; set; } = new();
}

public class CoreMetricsResponse
{
    [JsonProperty("machineTranslationSegments")]
    public long MachineTranslationSegments { get; set; }
}

public class WorkflowAssignmentsResponse
{
    [JsonProperty("jobs")]
    public List<WorkflowAssignmentJobResponse> Jobs { get; set; } = [];
}

public class WorkflowAssignmentJobResponse
{
    [JsonProperty("jobId")]
    public long JobId { get; set; }

    [JsonProperty("steps")]
    public List<WorkflowAssignmentStepResponse> Steps { get; set; } = [];
}

public class WorkflowAssignmentStepResponse
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("displayStepName")]
    public string DisplayStepName { get; set; } = string.Empty;

    [JsonProperty("stepReferenceName")]
    public string StepReferenceName { get; set; } = string.Empty;

    [JsonProperty("bundles")]
    public List<WorkflowAssignmentBundleResponse> Bundles { get; set; } = [];
}

public class WorkflowAssignmentBundleResponse
{
    [JsonProperty("from")]
    public int From { get; set; }

    [JsonProperty("userId")]
    public long? UserId { get; set; }

    [JsonProperty("userName")]
    public string? UserName { get; set; }
}
