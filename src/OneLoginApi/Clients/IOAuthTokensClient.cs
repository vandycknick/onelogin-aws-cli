using System.Threading.Tasks;
using OneLoginApi.Models;

namespace OneLoginApi.Clients
{
    public interface IOAuthTokensClient
    {
        Task<OAuthTokens> GenerateTokens(string clientId, string clientSecret);
    }
}
