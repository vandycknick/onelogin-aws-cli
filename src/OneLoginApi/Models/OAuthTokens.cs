using System;

namespace OneLoginApi.Models
{
    public class OAuthTokens
    {
        public string AccessToken { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int ExpiresIn { get; set; } // Seconds
        public string TokenType { get; set; } = null!;
        public int AccountId { get; set; }
    }
}
