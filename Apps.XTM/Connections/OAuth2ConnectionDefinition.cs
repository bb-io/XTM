using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System.Text;

namespace Apps.XTM.Connections
{
    public class OAuth2ConnectionDefinition : IConnectionDefinition
    {
        public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>()
        {
            new ConnectionPropertyGroup
            {
                Name = "Credentials",
                AuthenticationType = ConnectionAuthenticationType.Undefined,
                ConnectionUsage = ConnectionUsage.Actions,
                ConnectionProperties = new List<ConnectionProperty>()
                {
                    new ConnectionProperty("client"),
                    new ConnectionProperty("username"),
                    new ConnectionProperty("password"),
                    new ConnectionProperty("api_endpoint"),
                }
            },
        };

        public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
        {
            var client = values.First(v => v.Key == "client");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "client",
                client.Value
            );
            var username = values.First(v => v.Key == "username");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "username",
                username.Value
            );
            var password = values.First(v => v.Key == "password");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "password",
                password.Value
            );
            var url = values.First(v => v.Key == "api_endpoint").Value.TrimEnd('/') + "/project-manager-api/services/v2/customer/XTMWebService";
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                "api_endpoint",
                url
            );
        }
    }
}