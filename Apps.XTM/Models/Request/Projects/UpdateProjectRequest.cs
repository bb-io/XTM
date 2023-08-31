using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Projects;

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    [Display("Reference ID")] public string? ReferenceId { get; set; }
    
    [Display("Payment status")]
    [DataSource(typeof(PaymentStatusDataHandler))]
    public string? PaymentStatus { get; set; }
    
    [Display("Proposal approval status")] 
    [DataSource(typeof(ProposalApprovalStatusDataHandler))]
    public string? ProposalApprovalStatus { get; set; }
    
    [Display("Project manager ID")] 
    [JsonConverter(typeof(StringToIntConverter), nameof(ProjectManagerId))]
    public string? ProjectManagerId { get; set; }
    
    [Display("Subject matter ID")]
    [JsonConverter(typeof(StringToIntConverter), nameof(SubjectMatterId))]
    [DataSource(typeof(SubjectMatterDataHandler))]
    public string? SubjectMatterId { get; set; }
    
    [Display("Segment locking type")]
    [DataSource(typeof(SegmentLockingTypeDataHandler))]
    public string? SegmentLockingType { get; set; }
}