using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using OneLoginApi.Clients;
using OneLoginApi.Models;

namespace OneLoginApi.Authentication
{
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly IOAuthTokensClient _oauthClient;
        private readonly Credentials _credentials;
        private readonly SemaphoreSlim _oauthTokenLock = new SemaphoreSlim(1, 1);
        private OAuthCredentials _oauthCredentials = new OAuthCredentials();

        public AuthenticationHandler(IOAuthTokensClient oauthClient, Credentials credentials, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _oauthClient = oauthClient;
            _credentials = credentials;
        }

        private class OAuthCredentials
        {
            public Credentials Credentials { get; set; } = new Credentials(string.Empty);
            public DateTime Expires { get; set; } = DateTime.Now;
            public OAuthTokens? Tokens { get; set; }
        }

        private async Task<Credentials> GetOAuthTokenCredentials()
        {
            if (_credentials.Login is null)
            {
                throw new InvalidOperationException();
            }

            await _oauthTokenLock.WaitAsync();
            if (DateTime.Now > _oauthCredentials.Expires)
            {
                var tokens = await _oauthClient.GenerateTokens(_credentials.Login, _credentials.Password);
                var expires = tokens.CreatedAt.ToUniversalTime().AddSeconds(tokens.ExpiresIn);
                _oauthCredentials = new OAuthCredentials
                {
                    Credentials = new Credentials(tokens.AccessToken),
                    Expires = expires,
                    Tokens = tokens,
                };
            }
            _oauthTokenLock.Release();

            return _oauthCredentials.Credentials;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var credentials = _credentials;

            if (_credentials.AuthenticationType == AuthenticationType.Basic)
            {
                credentials = await GetOAuthTokenCredentials();
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", credentials.Password);
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}
