using Apps.XTM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.XTM.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>()
    {
        new()
        {
            Name = ConnectionTypes.Credentials,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionUsage = ConnectionUsage.Actions,
            ConnectionProperties = new List<ConnectionProperty>()
            {
                new(CredsNames.Client) { DisplayName = "Client" },
                new(CredsNames.UserId) { DisplayName = "User ID" },
                new(CredsNames.Password) { DisplayName = "Password", Sensitive = true },
                new(CredsNames.Url) { DisplayName = "URL" }
            }
        },
        new()
        {
            Name = ConnectionTypes.GeneratedToken,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>()
            {
                new(CredsNames.Token) { DisplayName = "Token", Sensitive = true },
                new(CredsNames.Url) { DisplayName = "URL" }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        var providers = new List<AuthenticationCredentialsProvider>();

        if (values.TryGetValue(CredsNames.Url, out var url))
        {
            var safeUrl = url.TrimEnd('/') + "/project-manager-api-rest";
            providers.Add(new AuthenticationCredentialsProvider(CredsNames.Url, safeUrl));
        }

        foreach (var kv in values.Where(x => x.Key != CredsNames.Url))
            providers.Add(new AuthenticationCredentialsProvider(kv.Key, kv.Value));

        var rawConnectionType = values[nameof(ConnectionPropertyGroup)];
        var connectionType = ConnectionTypes.SupportedConnectionTypes.Contains(rawConnectionType)
            ? rawConnectionType
            : throw new Exception($"Unknown connection type: {rawConnectionType}");

        providers.Add(new AuthenticationCredentialsProvider(CredsNames.ConnectionType, connectionType));
        return providers;
    }
}