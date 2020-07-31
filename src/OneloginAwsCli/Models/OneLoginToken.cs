using System;

namespace OneloginAwsCli.Models
{
    public class OneLoginToken
    {
        public string AccessToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ExpiresIn { get; set; } // seconds
        public string RefreshToken { get; set; } // Deprecated
        public string TokenType { get; set; }
        public int AccountId { get; set; }
    }
}
