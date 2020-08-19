using OneLoginApi;

namespace OneLoginAws.Services
{
    public class OneLoginClientFactory : IOneLoginClientFactory
    {
        public IOneLoginClient Create(string clientId, string clientSecret, string region) =>
            OneLoginClient.Create(clientId, clientSecret, region);
    }
}
