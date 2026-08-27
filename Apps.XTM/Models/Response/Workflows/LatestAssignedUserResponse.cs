using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows;

public class LatestAssignedUserResponse
{
    [Display("User name")]
    public string? UserName { get; set; }

    [Display("User ID")]
    public string? UserId { get; set; }
}

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
    public List<WorkflowAssignmentBundleResponse> Bundles { get; set; } = [];
}

public class WorkflowAssignmentBundleResponse
{
    public string? UserId { get; set; }

    public string? UserName { get; set; }
}
