using System;

namespace OneloginAwsCli.Exceptions
{
    public class MissingRequiredSettingsException : Exception
    {
        public MissingRequiredSettingsException(string? subdomain, string? clientId, string? clientSecret, string? profile, string? durationSeconds, string? awsAppId) : base() =>
            (Subdomain, ClientId, ClientSecret, Profile, DurationSeconds, AwsAppId) = (subdomain, clientId, clientSecret, profile, durationSeconds, awsAppId);
        public MissingRequiredSettingsException() : base()
        {
        }

        public MissingRequiredSettingsException(string message) : base(message)
        {
        }

        public MissingRequiredSettingsException(string message, Exception? inner) : base(message, inner)
        {
        }

        public string? Subdomain {get; }
        public string? ClientId { get; }
        public string? ClientSecret { get; }
        public string? Profile { get; }
        public string? DurationSeconds { get; }
        public string? AwsAppId { get; }
    }
}
