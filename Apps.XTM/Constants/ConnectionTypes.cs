namespace Apps.XTM.Constants;

public static class ConnectionTypes
{
    public const string Credentials = "Credentials";
    public const string GeneratedToken = "Generated token";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [Credentials, GeneratedToken];
}
