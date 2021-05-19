using System;
using System.Collections.Generic;
using System.Net.Http;
using OneLoginApi.Authentication;
using OneLoginApi.Clients;

namespace OneLoginApi
{
    public class OneLoginClient : IOneLoginClient
    {
        private static List<string> _validRegions = new List<string>
        {
            "us", "eu",
        };

        private static Uri CreateBaseAddress(string region)
        {
            if (region is null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (!_validRegions.Contains(region))
            {
                throw new ArgumentException($"Invalid region only `us` or `eu` allowed, but was given `{region}`.", nameof(region));
            }

            return new Uri($"https://api.{region}.onelogin.com");
        }

        public static OneLoginClient Create(string clientId, string clientSecret, string region)
        {
            if (clientId is null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (clientSecret is null)
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            if (region is null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            var credentials = new Credentials(clientId, clientSecret);
            return new OneLoginClient(credentials, region);
        }

        public OneLoginClient(Credentials credentials, string region) : this(new HttpClientHandler(), CreateBaseAddress(region), credentials)
        {

        }

        public OneLoginClient(HttpMessageHandler handler, Uri baseAddress, Credentials credentials)
        {
            BaseAddress = baseAddress;
            Credentials = credentials;
            OAuthTokens = new OAuthTokensClient(new HttpClient(handler) { BaseAddress = baseAddress });

            var authenticationHandler = new AuthenticationHandler(OAuthTokens, Credentials, handler);
            _client = new HttpClient(authenticationHandler)
            {
                BaseAddress = baseAddress
            };

            SAML = new SAMLClient(_client);
        }

        private readonly HttpClient _client;

        public Uri BaseAddress { get; private set; }
        public Credentials Credentials { get; private set; }

        public IOAuthTokensClient OAuthTokens { get; }
        public ISAMLClient SAML { get; }
    }
}
