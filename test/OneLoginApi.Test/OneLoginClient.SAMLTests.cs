using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OneLoginApi.Authentication;
using OneLoginApi.Exceptions;
using RichardSzalay.MockHttp;
using Xunit;

namespace OneLoginApi.Test
{
    public partial class OneLoginClientTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ThrowsArgumentExceptionWhenUsernameOrEmailIsNullOrEmpty(string usernameOrEmail)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.GenerateSamlAssertion(usernameOrEmail, "pwd", "appId", "subdomain"));

            // Then
            Assert.Equal("usernameOrEmail", exception.ParamName);
            Assert.Equal("'usernameOrEmail' cannot be null or empty (Parameter 'usernameOrEmail')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ThrowsArgumentExceptionWhenPasswordIsNullOrEmpty(string password)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.GenerateSamlAssertion("usernameOrEmail", password, "appId", "subdomain"));

            // Then
            Assert.Equal("password", exception.ParamName);
            Assert.Equal("'password' cannot be null or empty (Parameter 'password')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ThrowsArgumentExceptionWhenAppIdIsNullOrEmpty(string appId)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.GenerateSamlAssertion("usernameOrEmail", "password", appId, "subdomain"));

            // Then
            Assert.Equal("appId", exception.ParamName);
            Assert.Equal("'appId' cannot be null or empty (Parameter 'appId')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ThrowsArgumentExceptionWhenSubdomainIsNullOrEmpty(string subdomain)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.GenerateSamlAssertion("usernameOrEmail", "password", "appId", subdomain));

            // Then
            Assert.Equal("subdomain", exception.ParamName);
            Assert.Equal("'subdomain' cannot be null or empty (Parameter 'subdomain')", exception.Message);
        }

        [Fact]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ReturnsSuccessfullSAMLResponse()
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
                        ""access_token"": ""123"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""456x"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            handler
                .When("http://localhost/api/2/saml_assertion")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", "bearer 123"}
                })
                .Respond("application/json", @"
                    {
                        ""data"": ""PHNhbWw+c3VjY2Vzczwvc2FtbD4K"",
                        ""message"": ""Success""
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials(clientId, clientSecret));
            var saml = await client.SAML.GenerateSamlAssertion("test@test.com", "123", "456", "localhost");

            // Then
            Assert.Equal("Success", saml.Message);
            Assert.Equal("PHNhbWw+c3VjY2Vzczwvc2FtbD4K", saml.Data);

            handler.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ReturnsVerifyFactorChallenge()
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
                        ""access_token"": ""123"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""456x"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            handler
                .When("http://localhost/api/2/saml_assertion")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", "bearer 123"}
                })
                .Respond("application/json", @"
                    {
                        ""state_token"": ""4109"",
                        ""message"": ""MFA is required for this user"",
                        ""devices"": [
                            {
                                ""device_id"": 666666,
                                ""device_type"": ""Google Authenticator""
                            },
                            {
                                ""device_type"": ""Yubico YubiKey"",
                                ""device_id"": 1111111
                            }
                        ],
                        ""callback_url"": ""https://api.us.onelogin.com/api/2/saml_assertion/verify_factor"",
                        ""user"": {
                            ""lastname"": ""Zhang"",
                            ""username"": ""hzhang123"",
                            ""email"": ""hazel.zhang@onelogin.com"",
                            ""firstname"": ""Hazel"",
                            ""id"": 1
                        }
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials(clientId, clientSecret));
            var saml = await client.SAML.GenerateSamlAssertion("test@test.com", "123", "456", "localhost");

            // Then
            Assert.Equal("MFA is required for this user", saml.Message);
            Assert.Empty(saml.Data);
            Assert.Equal("4109", saml.StateToken);
            Assert.Equal(2, saml.Devices.Count);
            Assert.Equal("https://api.us.onelogin.com/api/2/saml_assertion/verify_factor", saml.CallbackUrl);
            Assert.Equal("Zhang", saml.User.Lastname);
            Assert.Equal("hzhang123", saml.User.Username);
            Assert.Equal("hazel.zhang@onelogin.com", saml.User.Email);
            Assert.Equal("Hazel", saml.User.Firstname);
            Assert.Equal(1, saml.User.Id);

            handler.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task OneLoginClient_SAML_GenerateSamlAssertion_ThrowsApiExceptionFor401()
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
                        ""access_token"": ""123"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""456x"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            handler
                .When("http://localhost/api/2/saml_assertion")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", "bearer 123"}
                })
                .Respond(HttpStatusCode.Unauthorized, "application/json", @"
                    {
                        ""message"": ""User is locked. Access is unauthorized"",
                        ""statusCode"": 401,
                        ""name"": ""Unauthorized""
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials(clientId, clientSecret));
            var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
                client.SAML.GenerateSamlAssertion("test@test.com", "123", "456", "localhost")
            );
            // Then
            Assert.Equal(401, exception.Error.StatusCode);
            Assert.Equal("Unauthorized", exception.Error.Name);
            Assert.Equal("User is locked. Access is unauthorized", exception.Error.Message);
            Assert.Equal("User is locked. Access is unauthorized", exception.Message);

            handler.VerifyNoOutstandingRequest();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_VerifyFactor_ThrowsArgumentExceptionWhenAppIdIsNullOrEmpty(string appId)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.VerifyFactor(appId, 123, "stateToken"));

            // Then
            Assert.Equal("appId", exception.ParamName);
            Assert.Equal("'appId' cannot be null or empty (Parameter 'appId')", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OneLoginClient_SAML_VerifyFactor_ThrowsArgumentExceptionWhenStateTokenIsNullOrEmpty(string stateToken)
        {
            // Given
            var handler = new MockHttpMessageHandler();

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials("hello", "my-secret"));
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SAML.VerifyFactor("appId", 123, stateToken));

            // Then
            Assert.Equal("stateToken", exception.ParamName);
            Assert.Equal("'stateToken' cannot be null or empty (Parameter 'stateToken')", exception.Message);
        }

        [Fact]
        public async Task OneLoginClient_SAML_VerifyFactor_ReturnsSuccessfullSAMLResponse()
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
                        ""access_token"": ""123"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""456x"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            handler
                .When("http://localhost/api/2/saml_assertion/verify_factor")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", "bearer 123"}
                })
                .Respond("application/json", @"
                    {
                        ""data"": ""PHNhbWw+c3VjY2Vzczwvc2FtbD4K"",
                        ""message"": ""Success""
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials(clientId, clientSecret));
            var factor = await client.SAML.VerifyFactor("123", 456, "stateToken", "otpToken");

            // Then
            Assert.Equal("Success", factor.Message);
            Assert.Equal("PHNhbWw+c3VjY2Vzczwvc2FtbD4K", factor.Data);

            handler.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task OneLoginClient_SAML_VerifyFactor_ThrowsApiExceptionFor401()
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
                        ""access_token"": ""123"",
                        ""created_at"": ""2015-11-11T03:36:18.714Z"",
                        ""expires_in"": 36000,
                        ""refresh_token"": ""456x"",
                        ""token_type"": ""bearer"",
                        ""account_id"": 555555
                    }
                ");

            handler
                .When("http://localhost/api/2/saml_assertion/verify_factor")
                .WithHeaders(new Dictionary<string, string>
                {
                    {"Authorization", "bearer 123"}
                })
                .Respond(HttpStatusCode.Unauthorized, "application/json", @"
                    {
                        ""message"": ""Failed authentication with this factor"",
                        ""statusCode"": 401,
                        ""name"": ""Unauthorized""
                    }
                ");

            // When
            var client = new OneLoginClient(handler, new Uri("http://localhost/"), new Credentials(clientId, clientSecret));
            var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
                client.SAML.VerifyFactor("123", 456, "stateToken", "otpToken")
            );
            // Then
            Assert.Equal(401, exception.Error.StatusCode);
            Assert.Equal("Unauthorized", exception.Error.Name);
            Assert.Equal("Failed authentication with this factor", exception.Error.Message);
            Assert.Equal("Failed authentication with this factor", exception.Message);

            handler.VerifyNoOutstandingRequest();
        }

    }
}
