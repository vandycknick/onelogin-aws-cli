using System.Threading.Tasks;
using OneloginAwsCli.Api.Models;

namespace OneloginAwsCli.Api
{
    public interface IOneLoginClient
    {
        OneLoginCredentials? Credentials { get; set; }
        string Region { get; set; }

        Task<OneLoginToken> GenerateTokens();
        Task<SAMLResponse> GenerateSamlAssertion(string usernameOrEmail, string password, string appId, string subdomain);
        Task<FactorResponse> VerifyFactor(string appId, int deviceId, string stateToken, string? otpToken = null);
    }
}
