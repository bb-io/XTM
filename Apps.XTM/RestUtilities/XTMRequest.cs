using Apps.XTM.Models.Request;
using RestSharp;

namespace Apps.XTM.RestUtilities;

public class XTMRequest : RestRequest
{
    public XTMRequest(XtmRequestParameters requestParameters, string token) : base(requestParameters.Url, requestParameters.Method)
    {
        this.AddHeader("Authorization", $"XTM-Basic {token}");
    }
    
}