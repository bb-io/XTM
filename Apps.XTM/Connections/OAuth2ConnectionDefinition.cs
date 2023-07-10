using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.XTM.Connections
{
    public class OAuth2ConnectionDefinition : IConnectionDefinition
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
                    new("client") { DisplayName = "Client" },
                    new("user_id") { DisplayName = "User id" },
                    new("password") { DisplayName = "Password" },
                    new("url") { DisplayName = "Url" },
                }
            },
        };

        public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
            Dictionary<string, string> values)
        {
            var client = values.First(v => v.Key == "client");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "client",
                client.Value
            );
            var username = values.First(v => v.Key == "user_id");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "user_id",
                username.Value
            );
            var password = values.First(v => v.Key == "password");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "password",
                password.Value
            );
            var url = values.First(v => v.Key == "url").Value.TrimEnd('/') + "/project-manager-api-rest";
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "url",
                url
            );
        }
    }
}