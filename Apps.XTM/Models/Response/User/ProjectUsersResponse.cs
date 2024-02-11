using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.User;

public class ProjectUsersResponse
{
    [Display("Linguists")]
    public List<UserResponse> Linguists { get; set; }
    [Display("Project manager")]
    public UserResponse ProjectManager { get; set; }
    [Display("Project creator")]
    public UserResponse ProjectCreator { get; set; }

    public ProjectUsersResponse()
    {
        Linguists = new();
    }
}