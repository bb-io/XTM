using Apps.XTM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.XTM.Extenstions
{
    public static class AuthProvidersExtension
    {
        public static string GetInstanceUrl(this IEnumerable<AuthenticationCredentialsProvider> source)
        {
            return source.FirstOrDefault(x => x.KeyName == CredsNames.Url)
                ?.Value ?? string.Empty;
        }
    }
}
