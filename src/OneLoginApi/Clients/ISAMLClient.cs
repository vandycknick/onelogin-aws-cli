using System.Threading.Tasks;
using OneLoginApi.Models;

namespace OneLoginApi.Clients
{
    public interface ISAMLClient
    {
        Task<SAMLResponse> GenerateSamlAssertion(string usernameOrEmail, string password, string appId, string subdomain);
        Task<FactorResponse> VerifyFactor(string appId, int deviceId, string stateToken, string? otpToken = null);
    }
}
