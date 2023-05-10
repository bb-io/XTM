using Blackbird.Applications.Sdk.Common.Authentication;
using ServiceReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM
{
    public class XTMClient : XTMCustomerMTOMWebServiceClient
    {
        public loginAPI Configuration { get; set; }

        public XTMClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) : 
            base(EndpointConfiguration.XTMCustomerMTOMWebServicePort, authenticationCredentialsProviders.First(p => p.KeyName == "api_endpoint").Value)
        {
            var client = authenticationCredentialsProviders.First(p => p.KeyName == "client").Value;
            var username = authenticationCredentialsProviders.First(p => p.KeyName == "username").Value;
            var password = authenticationCredentialsProviders.First(p => p.KeyName == "password").Value;
            Configuration = new loginAPI() 
            { 
                client = client,
                username = username,
                password = password
            };
        }
    }
}
