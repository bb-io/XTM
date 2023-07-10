using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.XTM.Extensions;

public static class CredsExtensions
{
    public static string Get(this IEnumerable<AuthenticationCredentialsProvider> creds, string key)
        => creds.First(x => x.KeyName == key).Value;
}