using System;

namespace OneloginAwsCli.Models
{
    public class Settings : ICloneable
    {
        public Uri BaseUri { get; set; }
        public string Subdomain { get; set; }
        public string Username { get; set; }
        internal string Password { get; set; } // Only used internally, not exposed via config setting.
        internal string OTP { get; set; } // Only used internally, not exposed via config setting.
        public string OTPDeviceId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Profile { get; set; }
        public string DurationSeconds { get; set; }
        public string AwsAppId { get; set; }
        public string RoleARN { get; set; }
        public string Region { get; set; }

        public object Clone() => MemberwiseClone();
    }
}
