namespace Apps.XTM.Models.Response.User;

public class ProjectUsers
{
    public List<ProjectUser> Linguists { get; set; }
    public ProjectUser ProjectManager { get; set; }
    public ProjectUser ProjectCreator { get; set; }
}