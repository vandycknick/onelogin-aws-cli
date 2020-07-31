using System.Collections.Generic;

namespace OneloginAwsCli.Models
{
    public class SAMLResponse
    {
        public string Message { get; set; }
        public string Data { get; set; }
        public string StateToken { get; set; }
        public List<Device> Devices { get; set; }
        public string CallbackUrl { get; set; }
        public User User { get; set; }
    }
}
