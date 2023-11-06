namespace Apps.XTM.Models.Response;

public class ErrorResponse
{
    public string Reason { get; set; }
    public string? IncorrectParameters { get; set; }
}