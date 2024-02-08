namespace Apps.XTM.Models.Response.User;

public class ProjectUsersResponse
{
    public List<UserResponse> Linguists { get; set; }
    public UserResponse ProjectManager { get; set; }
    public UserResponse ProjectCreator { get; set; }

    public ProjectUsersResponse()
    {
        Linguists = new();
    }
}