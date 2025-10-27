using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Models.Request.Customers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.TranslationMemory;

public class GenerateTMRequest : CustomerRequest
{
    [Display("Project ID")] 
    [DataSource(typeof(ProjectDataHandler))]
    public string? ProjectId { get; set; }
    
    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }
    
    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Output file type")]
    [DataSource(typeof(TmFileTypeDataHandler))]
    public string? fileType { get; set; }
}