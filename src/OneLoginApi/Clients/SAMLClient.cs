using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OneLoginApi.Helpers;
using OneLoginApi.Models;

namespace OneLoginApi.Clients
{
    public class SAMLClient : ApiClient, ISAMLClient
    {
        private const string SAML_ASSERTION_EP = "api/2/saml_assertion";
        private const string SAML_VERIFY_FACTOR_EP = "api/2/saml_assertion/verify_factor";
        private readonly HttpClient _client;

        public SAMLClient(HttpClient client) : base()
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        public async Task<SAMLResponse> GenerateSamlAssertion(string usernameOrEmail, string password, string appId, string subdomain)
        {
            if (string.IsNullOrEmpty(usernameOrEmail))
            {
                throw new ArgumentException($"'{nameof(usernameOrEmail)}' cannot be null or empty", nameof(usernameOrEmail));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException($"'{nameof(password)}' cannot be null or empty", nameof(password));
            }

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException($"'{nameof(appId)}' cannot be null or empty", nameof(appId));
            }

            if (string.IsNullOrEmpty(subdomain))
            {
                throw new ArgumentException($"'{nameof(subdomain)}' cannot be null or empty", nameof(subdomain));
            }

            var body = JsonSerializer.Serialize(new
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password,
                AppId = appId,
                Subdomain = subdomain,
            }, _options);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(SAML_ASSERTION_EP, content);

            await EnsureApiRequestSuccess(response);

            var result = await response.ReadAsAsync<SAMLResponse>(_options);
            return result;
        }

        public async Task<FactorResponse> VerifyFactor(string appId, int deviceId, string stateToken, string? otpToken = null)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException($"'{nameof(appId)}' cannot be null or empty", nameof(appId));
            }

            if (string.IsNullOrEmpty(stateToken))
            {
                throw new ArgumentException($"'{nameof(stateToken)}' cannot be null or empty", nameof(stateToken));
            }

            var body = JsonSerializer.Serialize(new
            {
                AppId = appId,
                DeviceId = $"{deviceId}",
                StateToken = stateToken,
                OtpToken = otpToken,
            }, _options);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(SAML_VERIFY_FACTOR_EP, content);

            await EnsureApiRequestSuccess(response);

            var result = await response.ReadAsAsync<FactorResponse>(_options);
            return result;
        }
    }
}
