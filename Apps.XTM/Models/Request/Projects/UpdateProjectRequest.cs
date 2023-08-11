using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Display("Reference id")] public string? ReferenceId { get; set; }
    
    [Display("Payment status")]
    [DataSource(typeof(PaymentStatusDataHandler))]
    public string? PaymentStatus { get; set; }
    
    [Display("Proposal approval status")] 
    [DataSource(typeof(ProposalApprovalStatusDataHandler))]
    public string? ProposalApprovalStatus { get; set; }
    
    [Display("Project manager id")] public int? ProjectManagerId { get; set; }
    [Display("Subject matter id")] public int? SubjectMatterId { get; set; }
    
    [Display("Segment locking type")]
    [DataSource(typeof(SegmentLockingTypeDataHandler))]
    public string? SegmentLockingType { get; set; }
}