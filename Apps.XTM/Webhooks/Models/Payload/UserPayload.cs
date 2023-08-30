using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class UserPayload
{
    [Display("User ID")]
    public string Id { get; set; }  
    
    [Display("User name")]
    public string Name { get; set; }   
}