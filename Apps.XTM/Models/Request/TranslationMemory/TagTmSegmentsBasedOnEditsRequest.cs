using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.TranslationMemory;

public class TagTmSegmentsBasedOnEditsRequest : CustomerRequest
{
    [Display("Project ID")]
    [DataSource(typeof(ProjectDataHandler))]
    public string? ProjectId { get; set; }

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Created by user IDs")]
    [DataSource(typeof(UserDataHandler))]
    public IEnumerable<string> CreatedByUserIds { get; set; } = [];

    [Display("Untrusted user IDs")]
    [DataSource(typeof(UserDataHandler))]
    public IEnumerable<string>? UntrustedUserIds { get; set; }

    [Display("Import project name")]
    public string ImportProjectName { get; set; } = string.Empty;

    [Display("Tag IDs")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? TagIds { get; set; }

    [Display("Created from")]
    public DateTime? CreatedDateFrom { get; set; }

    [Display("Created to")]
    public DateTime? CreatedDateTo { get; set; }

    [Display("Changed from")]
    public DateTime? ChangedDateFrom { get; set; }

    [Display("Changed to")]
    public DateTime? ChangedDateTo { get; set; }

    [Display("Approval status")]
    [DataSource(typeof(TmGenerationApprovalStatusDataHandler))]
    public string? ApprovalStatus { get; set; }

    [Display("Include reverse memory")]
    public bool? IncludeReverseMemory { get; set; }

    [Display("Dry run")]
    public bool? DryRun { get; set; }
}
