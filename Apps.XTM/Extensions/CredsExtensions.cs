﻿using System.Security.Cryptography;
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
    
    public static string GetInstanceUrl(this IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        return creds.FirstOrDefault(x => x.KeyName == CredsNames.Url)
            ?.Value ?? string.Empty;
    }
    
    public static string GetUrl(this IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var url = creds.Get(CredsNames.Url).TrimEnd('/');
        if(url.Contains("/project-manager-api-rest"))
            return url;
        
        return url + "/project-manager-api-rest";
    }
}