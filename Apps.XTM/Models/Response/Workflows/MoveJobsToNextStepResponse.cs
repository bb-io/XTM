using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Workflows;

public class MoveJobsToNextStepResponse
{
    [Display("Jobs")]
    public IEnumerable<JobStatus> Jobs { get; set; } = [];
}
