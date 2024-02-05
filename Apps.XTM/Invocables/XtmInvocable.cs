using Apps.XTM.Extenstions;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.Invocables;

public class XtmInvocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Creds 
        => InvocationContext.AuthenticationCredentialsProviders.ToArray();

    protected XTMClient Client { get; }
    protected string Url => Creds.GetInstanceUrl();
    protected string SoapUrl => Url.Replace("-rest", string.Empty).TrimEnd('/') + "/services/v2/projectmanager/XTMWebService";

    protected ProjectManagerMTOMWebServiceClient ProjectManagerMTOClient => new(ProjectManagerMTOMWebServiceClient.EndpointConfiguration.XTMProjectManagerMTOMWebServicePort, SoapUrl);

    protected XtmInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new();
    }

    protected long ParseId(string value)
    {
        return long.TryParse(value, out var result) 
            ? result 
            : throw new($"Failed to parse {value} to long");
    }
}