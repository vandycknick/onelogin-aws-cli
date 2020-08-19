using System.Collections.Generic;

namespace OneLoginApi.Models
{
    public class SAMLResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string StateToken { get; set; } = string.Empty;
        public List<Device> Devices { get; set; } = new List<Device>();
        public string CallbackUrl { get; set; } = string.Empty;
        public User User { get; set; } = null!;
    }
}
