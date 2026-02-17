using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Projects;

public class UpdateProjectRequest
{
    [Display("Name")]
    public string? Name { get; set; }

    [Display("Description")]
    public string? Description { get; set; }
    
    [Display("Reference ID")] 
    public string? ReferenceId { get; set; }
    
    [Display("Payment status")]
    [DataSource(typeof(PaymentStatusDataHandler))]
    public string? PaymentStatus { get; set; }
    
    [Display("Proposal approval status")] 
    [DataSource(typeof(ProposalApprovalStatusDataHandler))]
    public string? ProposalApprovalStatus { get; set; }
    
    [Display("Project manager ID")] 
    public string? ProjectManagerId { get; set; }
    
    [Display("Subject matter ID")]
    [DataSource(typeof(SubjectMatterDataHandler))]
    public string? SubjectMatterId { get; set; }
    
    [Display("Segment locking type")]
    [DataSource(typeof(SegmentLockingTypeDataHandler))]
    public string? SegmentLockingType { get; set; }

    [Display("Translation memory penalty profile ID")]
    [DataSource(typeof(TranslationMemoryPenaltyProfileDataHandler))]
    public string? TranslationMemoryPenaltyProfileId { get; set; }

    [Display("Translation memory tag IDs")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? TranslationMemoryTagIds { get; set; }

    [Display("Terminology penalty profile ID")]
    [DataSource(typeof(TerminologyPenaltyProfileDataHandler))]
    public string? TerminologyPenaltyProfileId { get; set; }

    [Display("Terminology tag IDs")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? TerminologyTagIds { get; set; }
}