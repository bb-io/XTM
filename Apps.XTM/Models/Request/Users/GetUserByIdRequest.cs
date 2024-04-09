using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Users;

public class GetUserByIdRequest
{
    [Display("User ID")]
    public string Id { get; set; }
}