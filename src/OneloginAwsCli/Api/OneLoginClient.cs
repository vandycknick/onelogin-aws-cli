using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OneloginAwsCli.Extensions;
using OneloginAwsCli.Api.Models;
using OneloginAwsCli.Api.Exceptions;
using System.Net;

namespace OneloginAwsCli.Api
{
    public class OneLoginClient : IOneLoginClient
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _options;

        private OneLoginToken? _internalToken;
        private DateTime _expires = DateTime.UtcNow;
        public OneLoginCredentials? Credentials { get; set; }
        public string Region { get; set; } = "us";

        public OneLoginClient(HttpClient client)
        {
            _client = client;
            _options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = new SnakeCaseNamingPolicy()
            };
        }

        // https://developers.onelogin.com/api-docs/1/oauth20-tokens/generate-tokens
        public async Task<OneLoginToken> GenerateTokens()
        {
            if (Credentials is null)
            {
                throw new InvalidOperationException("No credentials provided!");
            }

            var body = JsonSerializer.Serialize(new { GrantType = "client_credentials" }, _options);

            var message = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://api.{Region}.onelogin.com/auth/oauth2/v2/token")
            };

            message.Headers.TryAddWithoutValidation("Authorization", $"client_id:{Credentials.ClientId}, client_secret:{Credentials.ClientSecret}");
            message.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(message);

            await EnsureApiRequestSuccess(response);

            var result = await response.ReadAsAsync<OneLoginToken>(_options);
            return result;
        }

        private readonly SemaphoreSlim _refreshSyncLock = new SemaphoreSlim(1, 1);
        private async Task RefreshInternalToken()
        {
            // I need to sync access to the token refresh actions. I should only have one token
            // request in flight. Although doesn't really matter for the way I'm using it
            await _refreshSyncLock.WaitAsync();

            if (_internalToken is null || DateTime.Now > _expires)
            {
                _internalToken = await GenerateTokens();
                _expires = _internalToken.CreatedAt.ToUniversalTime().AddSeconds(_internalToken.ExpiresIn);
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_internalToken.TokenType, _internalToken.AccessToken);
            }

            _refreshSyncLock.Release();
        }

        // https://developers.onelogin.com/api-docs/2/saml-assertions/generate-saml-assertion
        public async Task<SAMLResponse> GenerateSamlAssertion(string usernameOrEmail, string password, string appId, string subdomain)
        {
            await RefreshInternalToken();

            var body = JsonSerializer.Serialize(new
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password,
                AppId = appId,
                Subdomain = subdomain,
            }, _options);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"https://api.{Region}.onelogin.com/api/2/saml_assertion", content);

            await EnsureApiRequestSuccess(response);

            var result = await response.ReadAsAsync<SAMLResponse>(_options);
            return result;
        }

        public async Task<FactorResponse> VerifyFactor(string appId, int deviceId, string stateToken, string? otpToken = null)
        {
            await RefreshInternalToken();

            var body = JsonSerializer.Serialize(new
            {
                AppId = appId,
                DeviceId = $"{deviceId}",
                StateToken = stateToken,
                OtpToken = otpToken,
            }, _options);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"https://api.{Region}.onelogin.com/api/2/saml_assertion/verify_factor", content);

            await EnsureApiRequestSuccess(response);

            var result = await response.ReadAsAsync<FactorResponse>(_options);
            return result;
        }

        public async Task EnsureApiRequestSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var responseContent = string.Empty;
            if (response.Content is object)
            {
                responseContent = await response.Content.ReadAsStringAsync();
            }

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthorizationException(responseContent),
                HttpStatusCode.NotFound => new NotFoundException(responseContent),
                _ => new ApiException(responseContent, response.StatusCode),
            };
        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return ToSnakeCase(name);
        }

        public static string ToSnakeCase(string str)
        {
            return string.Concat(
                str.Select(
                    (x, i) => i > 0 && char.IsUpper(x)
                        ? "_" + x
                        : x.ToString()
                        )
                ).ToLower();
        }
    }
}
