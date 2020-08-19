using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OneLoginApi.Authentication;
using OneLoginApi.Exceptions;
using OneLoginApi.Test.Extensions;
using RichardSzalay.MockHttp;
using Xunit;

namespace OneLoginApi.Test
{
    public partial class OneLoginClientTests
    {
        [Fact]
        public async Task OneLoginClient_OAuthTokens_GenerateTokens_ThrowsArgumentNullExceptionWhenClientIdIsNull()
        {
            // Given
            string clientId = null;
            string clientSecret = null;
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.OAuthTokens.GenerateTokens(clientId, clientSecret));

            // Then
            Assert.Equal("clientId", exception.ParamName);
        }

        [Fact]
        public async Task OneLoginClient_OAuthTokens_GenerateTokens_ThrowsArgumentNullExceptionWhenClientSecretIsNull()
        {
            // Given
            string clientId = "hello";
            string clientSecret = null;
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.OAuthTokens.GenerateTokens(clientId, clientSecret));

            // Then
            Assert.Equal("clientSecret", exception.ParamName);
        }

        [Fact]
        public async Task OneLoginClient_OAuthTokens_GenerateTokens_ReturnsOAuthTokensOnSuccessfullRequest()
        {
            // Given
            var clientId = "clientId";
            var clientSecret = "clientSecret";
            var handler = new MockHttpMessageHandler();

            handler
                .When("http://localhost/auth/oauth2/v2/token")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", $"client_id:{clientId}, client_secret:{clientSecret}"}
                })
                .Respond("application/json", @"
                    {
                        ""access_token"": ""xx508xx63817x752xx74004x30705xx92x58349x5x78f5xx34xxxxx51"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""628x9x0xx447xx4x421x517x4x474x33x2065x4x1xx523xxxxx6x7x20"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var tokens = await client.OAuthTokens.GenerateTokens(clientId, clientSecret);

            // Then
            Assert.Equal("xx508xx63817x752xx74004x30705xx92x58349x5x78f5xx34xxxxx51", tokens.AccessToken);
            Assert.Equal("2015-11-11T03:36:18.714Z".ToDateTime(), tokens.CreatedAt);
            Assert.Equal(36000, tokens.ExpiresIn);
            Assert.Equal("bearer", tokens.TokenType);
            Assert.Equal(555555, tokens.AccountId);

            handler.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task OneLoginClient_OAuthTokens_GenerateTokens_ThrowsApiExceptionFor401()
        {
            // Given
            var clientId = "clientId";
            var clientSecret = "clientSecret";
            var handler = new MockHttpMessageHandler();

            handler
                .When("http://localhost/auth/oauth2/v2/token")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", $"client_id:{clientId}, client_secret:{clientSecret}"}
                })
                .Respond(HttpStatusCode.Unauthorized, "application/json", @"
                    {
                        ""status"": {
                            ""error"": true,
                            ""code"": 401,
                            ""type"": ""Unauthorized"",
                            ""message"": ""Authentication Failure""
                        }
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
                client.OAuthTokens.GenerateTokens(clientId, clientSecret)
            );

            // Then
            Assert.Equal(401, exception.Error.StatusCode);
            Assert.Equal("Unauthorized", exception.Error.Name);
            Assert.Equal("Authentication Failure", exception.Error.Message);
            Assert.Equal("Authentication Failure", exception.Message);

            handler.VerifyNoOutstandingRequest();
        }
    }
}
