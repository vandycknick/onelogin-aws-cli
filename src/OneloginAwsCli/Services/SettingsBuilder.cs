using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
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

        public static void ThrowIfConfigFileMissing(IFileInfo file)
        {
            if (!file.Exists)
            {
                throw new ConfigFileNotFoundException(file.Name);
            }
        }

        public static List<string> GetConfigNames(IFileSystem fileSystem)
        {
            var file = fileSystem.FileInfo.FromFileName(ConfigFile);
            ThrowIfConfigFileMissing(file);

            using var reader = file.OpenText();
            return GetConfigNames(reader);
        }

        public static List<string> GetConfigNames(StreamReader reader)
        {
            var parser = new FileIniDataParser();
            var data = parser.ReadData(reader);
            return data.Sections.Select(section => section.SectionName).ToList();
        }

        private readonly IniData _iniConfigFile;
        private Uri? _baseUri;
        private string? _subdomain;
        private string? _username;
        private string? _password;
        private string? _otp;
        private string? _otpDeviceId;
        private string? _clientId;
        private string? _clientSecret;
        private string? _profile;
        private string? _durationSeconds;
        private string? _awsAppId;
        private string? _roleARN;
        private string? _region;

        public SettingsBuilder(IFileSystem fileSystem)
        {
            var file = fileSystem.FileInfo.FromFileName(ConfigFile);

            ThrowIfConfigFileMissing(file);

            using var reader = file.OpenText();
            var parser = new FileIniDataParser();
            _iniConfigFile = parser.ReadData(reader);
        }

        public SettingsBuilder UseDefaults() => UseConfigName("defaults");

        public SettingsBuilder UseFromEnvironment()
        {
            var configName = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME");

            UseConfigName(configName);

            _profile = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE") ?? _profile;
            _username = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME") ?? _username;
            _durationSeconds = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS") ?? _durationSeconds;

            return this;
        }

        public SettingsBuilder UseCommandLineOverrides(string? profile, string? userName, string? region)
        {
            _profile = profile ?? _profile;
            _username = userName ?? _username;
            _region = region ?? _region;
            return this;
        }

        public SettingsBuilder UseConfigName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return this;

            var data = _iniConfigFile[name];

            if (data["base_uri"] != null)
            {
                _baseUri = new Uri(data["base_uri"]);
            }

            _subdomain = data["subdomain"] ?? _subdomain;
            _username = data["username"] ?? _username;
            _otpDeviceId = data["otp_device_id"] ?? _otpDeviceId;
            _clientId = data["client_id"] ?? _clientId;
            _clientSecret = data["client_secret"] ?? _clientSecret;
            _profile = data["profile"] ?? _profile;
            _durationSeconds = data["duration_seconds"] ?? _durationSeconds;
            _awsAppId = data["aws_app_id"] ?? _awsAppId;
            _roleARN = data["role_arn"] ?? _roleARN;
            _region = data["region"] ?? _region;

            return this;
        }

        private class Credentials
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? OTP { get; set; }
        }

        public SettingsBuilder UseFromJsonInput(IStandardStreamReader reader)
        {
            var line = reader.ReadLine();

            if (line is null)
            {
                return this;
            }

            var creds = JsonSerializer.Deserialize<Credentials>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _username = creds?.Username ?? _username;
            _password = creds?.Password ?? _password;
            _otp = creds?.OTP ?? _otp;
            return this;
        }

        public Settings Build()
        {
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret) ||
                string.IsNullOrEmpty(_subdomain) || string.IsNullOrEmpty(_awsAppId) ||
                string.IsNullOrEmpty(_durationSeconds) || string.IsNullOrEmpty(_profile)
            )
            {
                ThrowMissingRequiredSettingsException();
            }

            return new Settings(
                baseUri: _baseUri,
                subdomain: _subdomain,
                username: _username,
                password: _password,
                otp: _otp,
                otpDeviceId: _otpDeviceId,
                clientId: _clientId,
                clientSecret: _clientSecret,
                profile: _profile,
                durationSeconds: _durationSeconds,
                awsAppId: _awsAppId,
                roleArn: _roleARN,
                region: _region
            );
        }

        [DoesNotReturn]
        public void ThrowMissingRequiredSettingsException() =>
            throw new MissingRequiredSettingsException(
                subdomain: _subdomain,
                clientId: _clientId,
                clientSecret: _clientSecret,
                profile: _profile,
                durationSeconds: _durationSeconds,
                awsAppId: _awsAppId
            );
    }
}
