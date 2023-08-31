using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.SubjectMatters;

public class SubjectMatterResponse
{
    [Display("Subject matter ID")]
    public string Id { get; set; }
    
    public string Activity { get; set; }
    
    public string Name { get; set; }
    
    [Display("Duration factor")]
    public double DurationFactor { get; set; }
    
    [Display("Price factor")]
    public double PriceFactor { get; set; }
}