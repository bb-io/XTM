namespace Apps.XTM.Models.Request;

public record TokenRequest(string Client, string Password, long UserId);