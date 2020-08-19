using System;
using OneLoginApi.Authentication;
using OneLoginApi.Clients;

namespace OneLoginApi
{
    public interface IOneLoginClient
    {
        Uri BaseAddress { get; }
        Credentials Credentials { get; }
        IOAuthTokensClient OAuthTokens { get; }
        ISAMLClient SAML { get; }
    }
}
