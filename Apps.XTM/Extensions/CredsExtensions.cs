using System.Security.Cryptography;
using System.Text;
using Apps.XTM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.XTM.Extensions;

public static class CredsExtensions
{
    public static string Get(this IEnumerable<AuthenticationCredentialsProvider> creds, string key)
        => creds.First(x => x.KeyName == key).Value;
    
    public static string GetInstanceUrlHash(this IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var hash = new StringBuilder();
        var instanceUrl = creds.Get(CredsNames.Url);
    
        var crypto = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(instanceUrl));
        foreach (byte theByte in crypto)
            hash.Append(theByte.ToString("x2"));
    
        return hash.ToString();
    }
}