namespace Apps.XTM.Polling.Models.Memory;

public class WorkflowStepCompletedMemory
{
    public string Configuration { get; set; } = string.Empty;

    // Only current matching projects are retained. Each set holds handled completion scopes.
    public Dictionary<string, HashSet<string>> CompletedGroups { get; set; } = [];
}
