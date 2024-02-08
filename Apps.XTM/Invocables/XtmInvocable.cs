using Apps.XTM.Extensions;
using Apps.XTM.RestUtilities;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using TermService;

namespace Apps.XTM.Invocables;

public class XtmInvocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Creds 
        => InvocationContext.AuthenticationCredentialsProviders.ToArray();

    protected XTMClient Client { get; }
    protected string Url => Creds.GetInstanceUrl();
    protected string ProjectManagerSoapUrl => Url.Replace("-rest", string.Empty).TrimEnd('/') + "/services/v2/projectmanager/XTMWebService";

    protected string TermSoapUrl => Url.Replace("-rest", string.Empty).TrimEnd('/') + "/services/v2/term/XTMTermWebService";

    protected ProjectManagerMTOMWebServiceClient ProjectManagerMTOClient => new(ProjectManagerMTOMWebServiceClient.EndpointConfiguration.XTMProjectManagerMTOMWebServicePort, ProjectManagerSoapUrl);

    protected TermWebServiceClient TermWebServiceClient => new(TermWebServiceClient.EndpointConfiguration.XTMTermWebServicePort, TermSoapUrl);

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