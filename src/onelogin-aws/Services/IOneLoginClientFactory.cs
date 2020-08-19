using OneLoginApi;

namespace OneLoginAws.Services
{
    public interface IOneLoginClientFactory
    {
        IOneLoginClient Create(string clientId, string clientSecret, string region);
    }
}
