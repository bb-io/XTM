using RestSharp;

namespace Apps.XTM.Models.Request;

public record XtmRequestParameters
{
    public string Url { get; set; }
    public Method Method { get; init; }
}