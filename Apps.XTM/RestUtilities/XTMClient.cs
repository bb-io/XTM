using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.XTM.RestUtilities
{
    public class XTMClient : RestClient
    {
        #region Common Actions

        public async Task<RestResponse> ExecuteXtm(string endpoint, Method method, object? bodyObj,
            AuthenticationCredentialsProvider[] creds)
        {
            var token = await GetToken(creds);

            var request = new XTMRequest(new()
            {
                Url = creds.Get("url") + endpoint,
                Method = method
            }, token);

            if (bodyObj is not null)
                request.AddJsonBody(bodyObj);

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

        public async Task<T> ExecuteXtm<T>(string endpoint, Method method, object? bodyObj,
            AuthenticationCredentialsProvider[] creds)
        {
            var token = await GetToken(creds);

            var request = new XTMRequest(new()
            {
                Url = creds.Get("url") + endpoint,
                Method = method
            }, token);

            if (bodyObj is not null)
                request.AddJsonBody(bodyObj);

            return await ExecuteXtm<T>(request);
        }

        public async Task<T> ExecuteXtm<T>(string endpoint, Method method, Dictionary<string, string> body,
            AuthenticationCredentialsProvider[] creds)
        {
            var token = await GetToken(creds);

            var request = new XTMRequest(new()
            {
                Url = creds.Get("url") + endpoint,
                Method = method
            }, token);

            body.ToList().ForEach(x => request.AddParameter(x.Key, x.Value, false));
            //request.AddHeader("Content-Type", "multipart/form-data");
            request.AlwaysMultipartFormData = true;

            return await ExecuteXtm<T>(request);
        }

        public async Task<T> ExecuteXtm<T>(XTMRequest request)
        {
            var response = await ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
                throw GetXtmError(response);

            return JsonConvert.DeserializeObject<T>(response.Content);
        }

        #endregion

        #region Utils

        private async Task<string> GetToken(AuthenticationCredentialsProvider[] creds)
        {
            var url = creds.Get("url");

            var client = creds.Get("client");
            var userId = creds.Get("user_id");
            var password = creds.Get("password");

            var request = new RestRequest(url + ApiEndpoints.Token, Method.Post);
            request.AddJsonBody(new TokenRequest(client, password, long.Parse(userId)));

            var response = await this.ExecuteAsync<TokenResponse>(request);

            return response.Data.Token;
        }

        private Exception GetXtmError(RestResponse response)
        {
            var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);

            return new(error.Reason);
        }

        #endregion
    }
}