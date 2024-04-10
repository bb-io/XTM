﻿using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;

namespace Apps.XTM.RestUtilities;

public class XTMClient : RestClient
{
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
        var response = await ExecuteAsync(request);

        if (!response.IsSuccessStatusCode)
            throw GetXtmError(response);

        return response;
    }

    #endregion
        
    #region Generic Actions

    public async Task<T> ExecuteXtmWithJson<T>(string endpoint, Method method, object? bodyObj,
        AuthenticationCredentialsProvider[] creds)
    {
        var response = await ExecuteXtmWithJson(endpoint, method, bodyObj, creds);
        return JsonConvert.DeserializeObject<T>(response.Content);
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
        return JsonConvert.DeserializeObject<T>(response.Content);
    }

    #endregion

    #region Utils

    public async Task<string> GetToken(AuthenticationCredentialsProvider[] creds)
    {
        var url = creds.GetUrl();

        var client = creds.Get(CredsNames.Client);
        var userId = creds.Get(CredsNames.UserId);
        var password = creds.Get(CredsNames.Password);

        var request = new RestRequest(url + ApiEndpoints.Token, Method.Post);
        request.AddJsonBody(new TokenRequest(client, password, long.Parse(userId)));

        var response = await this.ExecuteAsync<TokenResponse>(request);

        return response.Data.Token;
    }

    private Exception GetXtmError(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
        var message = (error?.Reason.TrimEnd('.') ?? response.StatusCode.ToString())
                      + (error?.IncorrectParameters != null
                          ? ": " + error.IncorrectParameters.ToLower().Replace("_", " ") + "."
                          : ".");
        return new(message);
    }

    #endregion
}