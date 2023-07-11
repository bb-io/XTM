using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.TranslationMemory;

public class GenerateTMRequest
{
    [Display("Customer id")] public int CustomerId { get; set; }
    [Display("Project id")] public int? ProjectId { get; set; }
    [Display("Source language")] public string? SourceLanguage { get; set; }
    [Display("Target language")] public string? TargetLanguage { get; set; }
}