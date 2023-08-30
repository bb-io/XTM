using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.System;

public class SystemResponse
{
    [Display("Company name")]
    public string CompanyName { get; set; }
    
    public string Website { get; set; }
    
    public string Logo { get; set; }
    
    public string Version { get; set; }
}