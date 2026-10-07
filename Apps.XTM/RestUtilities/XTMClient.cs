using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Apps.XTM.RestUtilities;

public class XTMClient : RestClient
{
    private const int RetryCount = 4;
    private const int MaxBackoffSeconds = 16;
    private const int MaxRetryAfterSeconds = 60;

    private static readonly HttpStatusCode[] TransientGatewayStatusCodes =
    [
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private static readonly AsyncRetryPolicy<RestResponse> RateLimitRetryPolicy = Policy
        .HandleResult<RestResponse>(IsRateLimited)
        .WaitAndRetryAsync(RetryCount, (attempt, result, _) => GetRetryDelay(attempt, result.Result),
            (_, _, _, _) => Task.CompletedTask);

    private static readonly AsyncRetryPolicy<RestResponse> ReadRetryPolicy = Policy
        .HandleResult<RestResponse>(response => IsRateLimited(response) || IsTransientFailure(response))
        .WaitAndRetryAsync(RetryCount, (attempt, result, _) => GetRetryDelay(attempt, result.Result),
            (_, _, _, _) => Task.CompletedTask);

    #region Common Actions

    public async Task<RestResponse> ExecuteXtmWithJson(string endpoint, Method method, object? bodyObj,
        AuthenticationCredentialsProvider[] creds)
    {
        var token = await GetToken(creds);

        var request = new XTMRequest(new()
        {
            Url = creds.Get(CredsNames.Url) + endpoint,
            Method = method
        }, token);

        if (bodyObj is not null)
            request.WithJsonBody(bodyObj, new()
            {
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore
            });


        return await ExecuteXtm(request);
    }

    public async Task<RestResponse> ExecuteXtm(XTMRequest request)
    {
        var retryPolicy = request.Method == Method.Get ? ReadRetryPolicy : RateLimitRetryPolicy;
        var response = await retryPolicy.ExecuteAsync(() => ExecuteAsync(request));

        if (response.RawBytes != null && Encoding.UTF8.GetString(response.RawBytes).Contains("CANNOT_FIND_THE_FILE"))
            throw new PluginApplicationException("The file was not found, please check your input and try again");

        if (IsTransportFailure(response))
            throw new PluginApplicationException(GetTransportErrorMessage(request, response));

        if (!response.IsSuccessStatusCode)
            throw new PluginApplicationException(GetXtmError(response).Message);

        return response;
    }

    #endregion
        
    #region Generic Actions

    public async Task<T> ExecuteXtmWithJson<T>(string endpoint, Method method, object? bodyObj,
        AuthenticationCredentialsProvider[] creds)
    {
        var response = await ExecuteXtmWithJson(endpoint, method, bodyObj, creds);
        return JsonConvert.DeserializeObject<T>(response.Content ?? string.Empty);
    }

    public async Task<T> ExecuteXtmWithFormData<T>(string endpoint, Method method, Dictionary<string, string> body,
        AuthenticationCredentialsProvider[] creds)
    {
        var token = await GetToken(creds);

        var request = new XTMRequest(new()
        {
            Url = creds.Get(CredsNames.Url) + endpoint,
            Method = method
        }, token);

        body.ToList().ForEach(x => request.AddParameter(x.Key, x.Value, false));
        request.AlwaysMultipartFormData = true;

        return await ExecuteXtm<T>(request);
    }

    public async Task<T> ExecuteXtm<T>(XTMRequest request)
    {
        var response = await ExecuteXtm(request);
        return JsonConvert.DeserializeObject<T>(response.Content ?? string.Empty);
    }

    #endregion

    #region Utils

    public async Task<string> GetToken(AuthenticationCredentialsProvider[] creds)
    {
        if (creds.Get(CredsNames.ConnectionType) == ConnectionTypes.Credentials)
        {
            var url = creds.Get(CredsNames.Url);

            var client = creds.Get(CredsNames.Client);
            var userId = creds.Get(CredsNames.UserId);
            var password = creds.Get(CredsNames.Password);

            if (!long.TryParse(userId, out var parsedUserId))
                throw new PluginApplicationException($"Invalid user ID provided: '{userId}'. It must be a valid number");

            var request = new RestRequest(url + ApiEndpoints.Token, Method.Post);
            request.AddJsonBody(new TokenRequest(client, password, parsedUserId));

            var response = await ExecuteAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new PluginApplicationException(GetXtmError(response).Message);

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content ?? string.Empty);
            return tokenResponse!.Token;
        }
        else
            return creds.Get(CredsNames.Token);
    }

    private static bool IsRateLimited(RestResponse response) =>
        response.StatusCode == HttpStatusCode.TooManyRequests;

    private static bool IsTransportFailure(RestResponse response) =>
        response.StatusCode == 0 && response.ResponseStatus != ResponseStatus.Completed;

    private static bool IsTransientFailure(RestResponse response) =>
        IsTransportFailure(response) || TransientGatewayStatusCodes.Contains(response.StatusCode);

    private static TimeSpan GetRetryDelay(int retryAttempt, RestResponse response)
    {
        var retryAfter = response.Headers?
            .FirstOrDefault(h => string.Equals(h.Name, "Retry-After", StringComparison.OrdinalIgnoreCase))?
            .Value?.ToString();

        if (int.TryParse(retryAfter, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetryAfterSeconds));

        var backoffSeconds = Math.Min(Math.Pow(2, retryAttempt), MaxBackoffSeconds);
        return TimeSpan.FromSeconds(backoffSeconds) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
    }

    private static string GetTransportErrorMessage(RestRequest request, RestResponse response)
    {
        var path = Uri.TryCreate(request.Resource, UriKind.Absolute, out var uri) ? uri.AbsolutePath : request.Resource;
        var target = $"{request.Method.ToString().ToUpperInvariant()} {path}";

        return response.ResponseStatus == ResponseStatus.TimedOut
            ? $"XTM did not respond in time to {target}. The XTM server may be busy, please try again later."
            : $"Could not reach XTM for {target}: {response.ErrorMessage ?? response.ResponseStatus.ToString()}";
    }

    private static Exception GetXtmError(RestResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
            throw new PluginApplicationException(
                $"XTM returned {(int)response.StatusCode} ({response.StatusCode}) without an error description.");

        if (response.ContentType?.Contains("html") == true)
            throw new PluginApplicationException(ExtractHtmlErrorMessage(response.Content));

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
        var message = (error?.Reason.TrimEnd('.') ?? response.StatusCode.ToString())
                      + (error?.IncorrectParameters != null
                          ? ": " + error.IncorrectParameters.ToLower().Replace("_", " ") + "."
                          : ".");
        throw new PluginApplicationException($"Error: {message}");
    }

    private static string ExtractHtmlErrorMessage(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return "Empty HTML response received.";

        try
        {
            var titleMatch = Regex.Match(htmlContent, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : null;

            var h1Match = Regex.Match(htmlContent, @"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var h1 = h1Match.Success ? StripHtmlTags(h1Match.Groups[1].Value).Trim() : null;

            var errorPatterns = new[]
            {
                @"<[^>]*class[^>]*error[^>]*>(.*?)</[^>]*>",
                @"<[^>]*class[^>]*message[^>]*>(.*?)</[^>]*>",
                @"<p[^>]*>(.*?)</p>",
                @"<div[^>]*>(.*?)</div>"
            };

            string errorMessage = null;
            foreach (var pattern in errorPatterns)
            {
                var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    errorMessage = StripHtmlTags(match.Groups[1].Value).Trim();
                    if (errorMessage.Length > 10)
                        break;
                }
            }

            var messageParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(title) && !title.Contains("DOCTYPE") && !title.Contains("html"))
                messageParts.Add($"Page title: {title}");

            if (!string.IsNullOrWhiteSpace(h1) && h1 != title)
                messageParts.Add($"Error: {h1}");

            if (!string.IsNullOrWhiteSpace(errorMessage) && errorMessage != title && errorMessage != h1)
                messageParts.Add($"Details: {errorMessage}");

            if (messageParts.Any())
                return string.Join(" | ", messageParts);

            var cleanContent = StripHtmlTags(htmlContent).Trim();
            if (cleanContent.Length > 200)
                cleanContent = cleanContent.Substring(0, 200) + "...";

            return string.IsNullOrWhiteSpace(cleanContent)
                ? "Received HTML response without readable content."
                : $"HTML content: {cleanContent}";
        }
        catch (Exception ex)
        {
            return $"Could not parse HTML response. Error: {ex.Message}";
        }
    }

    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var withoutTags = Regex.Replace(html, @"<[^>]*>", " ");

        withoutTags = WebUtility.HtmlDecode(withoutTags);
        withoutTags = Regex.Replace(withoutTags, @"\s+", " ");

        return withoutTags.Trim();
    }

    #endregion
}