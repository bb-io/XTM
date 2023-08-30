using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.User;

public class UserResponse
{
    [Display("User ID")]
    public string Id { get; set; }
    
    public string Username { get; set; }
    
    [Display("First name")]
    public string FirstName { get; set; }
    
    [Display("Last name")]
    public string LastName { get; set; }
    
    [Display("User type")]
    public string UserType { get; set; }
}