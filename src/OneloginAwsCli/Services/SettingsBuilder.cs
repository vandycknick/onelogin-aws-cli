using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IniParser;
using IniParser.Model;
using OneloginAwsCli.Console;
using OneloginAwsCli.Exceptions;
using OneloginAwsCli.Models;

namespace OneloginAwsCli.Services
{
    public class SettingsBuilder : ISettingsBuilder
    {
        private const string CONFIG_FILE_NAME = ".onelogin-aws.config";

        public static string ConfigFile
        {
            get => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), CONFIG_FILE_NAME);
        }

        public static bool ConfigFileExists() => File.Exists(ConfigFile);

        public static List<string> GetConfigNames()
        {
            ThrowIfConfigFileIsMissing();

            var filePath = Path.Join(ConfigFile);
            using var reader = File.OpenText(filePath);
            return GetConfigNames(reader);
        }

        public static List<string> GetConfigNames(StreamReader reader)
        {
            var parser = new FileIniDataParser();
            var data = parser.ReadData(reader);
            return data.Sections.Select(section => section.SectionName).ToList();
        }

        private Settings _settings = new Settings();
        private readonly IniData _iniConfigFile;

        public SettingsBuilder()
        {
            ThrowIfConfigFileIsMissing();

            var filePath = Path.Join(ConfigFile);
            using var reader = File.OpenText(filePath);
            var parser = new FileIniDataParser();
            _iniConfigFile = parser.ReadData(reader);
        }

        public SettingsBuilder UseDefaults() => UseConfigName("defaults");

        public SettingsBuilder UseFromEnvironment()
        {
            var configName = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME");

            if (configName != null) UseConfigName(configName);

            _settings.Profile = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE") ?? _settings.Profile;
            _settings.Username = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME") ?? _settings.Username;
            _settings.DurationSeconds = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS") ?? _settings.DurationSeconds;

            return this;
        }

        public SettingsBuilder UseCommandLineOverrides(string profile, string userName, string region)
        {
            _settings.Profile = profile ?? _settings.Profile;
            _settings.Username = userName ?? _settings.Username;
            _settings.Region = region ?? _settings.Region;
            return this;
        }

        public SettingsBuilder UseConfigName(string name)
        {
            if (string.IsNullOrEmpty(name)) return this;

            var data = _iniConfigFile[name];

            if (data["base_uri"] != null)
            {
                _settings.BaseUri = new Uri(data["base_uri"]);
            }

            _settings.Subdomain = data["subdomain"] ?? _settings.Subdomain;
            _settings.Username = data["username"] ?? _settings.Username;
            _settings.OTPDeviceId = data["otp_device_id"] ?? _settings.OTPDeviceId;
            _settings.ClientId = data["client_id"] ?? _settings.ClientId;
            _settings.ClientSecret = data["client_secret"] ?? _settings.ClientSecret;
            _settings.Profile = data["profile"] ?? _settings.Profile;
            _settings.DurationSeconds = data["duration_seconds"] ?? _settings.DurationSeconds;
            _settings.AwsAppId = data["aws_app_id"] ?? _settings.AwsAppId;
            _settings.RoleARN = data["role_arn"] ?? _settings.RoleARN;
            _settings.Region = data["region"] ?? _settings.Region;

            return this;
        }

        private class Credentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string OTP { get; set; }
        }

        public SettingsBuilder UseFromJsonInput(IStandardStreamReader reader)
        {
            var line = reader.ReadLine();

            var creds = JsonSerializer.Deserialize<Credentials>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _settings.Username = creds.Username ?? _settings.Username;
            _settings.Password = creds.Password;
            _settings.OTP = creds.OTP;
            return this;
        }

        public Settings Build()
        {
            if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.ClientSecret) ||
                string.IsNullOrEmpty(_settings.Subdomain) || string.IsNullOrEmpty(_settings.AwsAppId) ||
                string.IsNullOrEmpty(_settings.DurationSeconds) || string.IsNullOrEmpty(_settings.Profile)
            )
            {
                ThrowMissingRequiredSettingsException(_settings);
            }

            return (Settings)_settings.Clone();
        }

        public static void ThrowMissingRequiredSettingsException(Settings settings) =>
            throw new MissingRequiredSettingsException
            {
                Settings = (Settings)settings.Clone()
            };

        public static void ThrowIfConfigFileIsMissing()
        {
            if (!ConfigFileExists())
            {
                throw new ConfigFileNotFoundException
                {
                    FilePath = ConfigFile,
                };
            }
        }
    }
}
