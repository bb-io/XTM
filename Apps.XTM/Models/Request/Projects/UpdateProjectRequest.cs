using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Display("Reference id")] public string? ReferenceId { get; set; }
    [Display("Payment status")] public string? PaymentStatus { get; set; }
    [Display("Proposal approval status")] public string? ProposalApprovalStatus { get; set; }
    [Display("Project manager id")] public int? ProjectManagerId { get; set; }
    [Display("Subject matter id")] public int? SubjectMatterId { get; set; }
    [Display("Segment locking type")] public string? SegmentLockingType { get; set; }
}