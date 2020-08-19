using System;

namespace OneLoginAws.Exceptions
{
    public class MissingRequiredSettingsException : Exception
    {
        public MissingRequiredSettingsException(string? baseUri, string? subdomain, string? clientId, string? clientSecret, string? profile, string? awsAppId) : base() =>
            (BaseUri, Subdomain, ClientId, ClientSecret, Profile, AwsAppId) = (baseUri, subdomain, clientId, clientSecret, profile, awsAppId);
        public MissingRequiredSettingsException() : base()
        {
        }

        public MissingRequiredSettingsException(string message) : base(message)
        {
        }

        public MissingRequiredSettingsException(string message, Exception? inner) : base(message, inner)
        {
        }

        public string? BaseUri { get; set; }
        public string? Subdomain {get; }
        public string? ClientId { get; }
        public string? ClientSecret { get; }
        public string? Profile { get; }
        public string? AwsAppId { get; }
    }
}
