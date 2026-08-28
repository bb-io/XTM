namespace Apps.XTM.Models.Response.Workflows;

public class WorkflowAssignmentsResponse
{
    public List<WorkflowAssignmentJobResponse> Jobs { get; set; } = [];
}

public class WorkflowAssignmentJobResponse
{
    public string JobId { get; set; } = string.Empty;

    public List<WorkflowAssignmentStepResponse> Steps { get; set; } = [];
}

public class WorkflowAssignmentStepResponse
{
    public string? Name { get; set; }

    public string? DisplayStepName { get; set; }

    public string? ReferenceStepName { get; set; }

    public List<WorkflowAssignmentBundleResponse> Bundles { get; set; } = [];
}

public class WorkflowAssignmentBundleResponse
{
    public long? Id { get; set; }

    public int? From { get; set; }

    public int? To { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }
}
