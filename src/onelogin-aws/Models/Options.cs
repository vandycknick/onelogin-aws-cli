namespace OneLoginAws.Models
{
    public record Options(string BaseUri, string Subdomain, string ClientId, string ClientSecret, string Profile, string DurationSeconds, string AwsAppId)
    {
        public string? Username { get; init; }
        public string? Password { get; init; } // Only used internally, not exposed via config setting.
        public string? OTP { get; init; } // Only used internally, not exposed via config setting.
        public string? OTPDeviceId { get; init; }
        public string? RoleARN { get; init; }
        public string? Region { get; init; }
        public void Deconstruct(out string? username, out string? password, out string? otp, out string? otpDeviceId) =>
            (username, password, otp, otpDeviceId) = (Username, Password, OTP, OTPDeviceId);
    }
}
