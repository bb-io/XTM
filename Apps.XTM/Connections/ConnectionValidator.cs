using Apps.XTM.Constants;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.XTM.Connections;

public class ConnectionValidator : IConnectionValidator
{
    private XTMClient Client => new();

    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        try
        {
            await Client.ExecuteXtmWithJson(ApiEndpoints.System,
                Method.Get,
                null,
                authProviders.ToArray());

            return new() { IsValid = true };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}