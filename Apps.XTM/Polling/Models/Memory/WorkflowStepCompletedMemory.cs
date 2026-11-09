using Apps.XTM.Polling.Models.Response;

namespace Apps.XTM.Polling.Models.Memory;

public class WorkflowStepCompletedMemory
{
    public string Configuration { get; set; } = string.Empty;

    // Only current matching projects are retained. Sets include returned and queued completion scopes.
    public Dictionary<string, HashSet<string>> CompletedGroups { get; set; } = [];

    public List<WorkflowStepCompletedResponse> PendingCompletions { get; set; } = [];

    public long? LastScannedProjectId { get; set; }

    public HashSet<string> ProjectsToRetry { get; set; } = [];

    public HashSet<string> ProjectsToBaseline { get; set; } = [];
}
