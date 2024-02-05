﻿using Apps.XTM.Extenstions;
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
    protected string SoapUrl => Url.Replace("api-rest", "gui").TrimEnd('/') + "/services/v2/projectmanager/XTMWebService";

    protected ProjectManagerMTOMWebServiceClient ProjectManagerMTOClient => new(ProjectManagerMTOMWebServiceClient.EndpointConfiguration.XTMProjectManagerMTOMWebServicePort, SoapUrl);

    protected XtmInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new();
    }
}