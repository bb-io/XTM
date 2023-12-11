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
            Name = "Credentials",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionUsage = ConnectionUsage.Actions,
            ConnectionProperties = new List<ConnectionProperty>()
            {
                new(CredsNames.Client) { DisplayName = "Client" },
                new(CredsNames.UserId) { DisplayName = "User ID" },
                new(CredsNames.Password) { DisplayName = "Password", Sensitive = true },
                new(CredsNames.Url) { DisplayName = "URL" }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        var client = values.First(v => v.Key == CredsNames.Client);
        yield return new AuthenticationCredentialsProvider(
            AuthenticationCredentialsRequestLocation.None,
            client.Key,
            client.Value
        );
        var username = values.First(v => v.Key == CredsNames.UserId);
        yield return new AuthenticationCredentialsProvider(
            AuthenticationCredentialsRequestLocation.None,
            username.Key,
            username.Value
        );
        var password = values.First(v => v.Key == CredsNames.Password);
        yield return new AuthenticationCredentialsProvider(
            AuthenticationCredentialsRequestLocation.None,
            password.Key,
            password.Value
        );
        var url = values.First(v => v.Key == CredsNames.Url).Value.TrimEnd('/') + "/project-manager-api-rest";
        yield return new AuthenticationCredentialsProvider(
            AuthenticationCredentialsRequestLocation.None,
            CredsNames.Url,
            url
        );
    }
}