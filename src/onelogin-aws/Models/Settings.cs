using System;

namespace OneLoginAws.Models
{
    public class Settings
    {
        public Settings(
            Uri? baseUri, string subdomain, string? username, string? password,
            string? otp, string? otpDeviceId, string clientId, string clientSecret,
            string profile, string durationSeconds, string awsAppId,
            string? roleArn, string? region) =>
            (
                BaseUri, Subdomain, Username, Password,
                OTP, OTPDeviceId, ClientId, ClientSecret,
                Profile, DurationSeconds, AwsAppId,
                RoleARN, Region
            ) = (
                baseUri, subdomain, username, password,
                otp, otpDeviceId, clientId, clientSecret,
                profile, durationSeconds, awsAppId,
                roleArn, region
            );

        public Uri? BaseUri { get; }
        public string Subdomain { get; }
        public string? Username { get; }
        internal string? Password { get; } // Only used internally, not exposed via config setting.
        internal string? OTP { get; } // Only used internally, not exposed via config setting.
        public string? OTPDeviceId { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string Profile { get; }
        public string DurationSeconds { get; }
        public string AwsAppId { get; }
        public string? RoleARN { get; }
        public string? Region { get; }

        public void Deconstruct(out string? username, out string? password, out string? otp, out string? otpDeviceId ) =>
            (username, password, otp, otpDeviceId) = (Username, Password, OTP, OTPDeviceId);
    }
}
