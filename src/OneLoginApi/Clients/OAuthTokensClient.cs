using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OneLoginApi.Exceptions;
using OneLoginApi.Helpers;
using OneLoginApi.Models;

namespace OneLoginApi.Clients
{
    internal class OAuthTokensClient : ApiClient, IOAuthTokensClient
    {
        private readonly HttpClient _client;
        private const string OAUTH_TOKEN_EP = "auth/oauth2/v2/token";

        public OAuthTokensClient(HttpClient client) : base()
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        public async Task<OAuthTokens> GenerateTokens(string clientId, string clientSecret)
        {
            if (clientId is null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (clientSecret is null)
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            var body = JsonSerializer.Serialize(new { GrantType = "client_credentials" }, _options);

            var message = new HttpRequestMessage(HttpMethod.Post, OAUTH_TOKEN_EP);

            message.Headers.TryAddWithoutValidation("Authorization", $"client_id:{clientId}, client_secret:{clientSecret}");
            message.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(message);

            await EnsureApiRequestSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<OAuthTokens>(_options);

            if (result is null)
            {
                throw new ApiException("Empty authorization tokens returned from server.", HttpStatusCode.BadRequest);
            }

            return result;
        }

        private class WrappedApiError
        {
            public JsonElement Status { get; set; }
        }

        protected override async Task EnsureApiRequestSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var error = new ApiError
            {
                Name = "",
                StatusCode = 0,
                Message = "",
            };

            try
            {
                var wrapped = await response.Content.ReadFromJsonAsync<WrappedApiError>(_errorOptions);

                error.Name = wrapped?.Status.GetProperty("type").GetString() ?? "";
                error.StatusCode = wrapped?.Status.GetProperty("code").GetInt32() ?? 0;
                error.Message = wrapped?.Status.GetProperty("message").GetString() ?? "";
            }
            catch (Exception)
            { }

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthorizationException(error),
                HttpStatusCode.NotFound => new NotFoundException(error),
                _ => new ApiException(error),
            };
        }
    }
}
