using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using IniParser;
using IniParser.Model;
using OneLoginAws.Exceptions;
using OneLoginAws.Models;

namespace OneLoginAws.Services
{
    public sealed class OptionsBuilder
    {
        private const string CONFIG_FILE_NAME = ".onelogin-aws.config";
        private const string CONFIG_FOLDER = "onelogin-aws";

        public static string ConfigFile => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), CONFIG_FILE_NAME);

        public static void ThrowIfConfigFileMissing(IFileInfo file)
        {
            if (!file.Exists)
            {
                throw new ConfigFileNotFoundException(ConfigFile);
            }
        }

        public static IFileInfo FindConfigFile(IFileSystem fileSystem)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Join(userHome, ".config");

            var file = fileSystem.FileInfo.FromFileName(Path.Join(xdgHome, CONFIG_FOLDER, "config"));

            if (file.Exists)
            {
                return file;
            }

            file = fileSystem.FileInfo.FromFileName(Path.Join(userHome, CONFIG_FILE_NAME));

            ThrowIfConfigFileMissing(file);

            return file;
        }

        public static List<string> GetConfigNames(IFileSystem fileSystem)
        {
            var file = FindConfigFile(fileSystem);
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
        private string? _baseUri;
        private string? _subdomain;
        private string? _username;
        private string? _password;
        private string? _otp;
        private string? _otpDeviceId;
        private string? _clientId;
        private string? _clientSecret;
        private string? _profile;
        private string _durationSeconds = "3600";
        private string? _awsAppId;
        private string? _roleARN;
        private string? _region;

        public OptionsBuilder(IFileSystem fileSystem)
        {
            var file = FindConfigFile(fileSystem);
            using var reader = file.OpenText();
            var parser = new FileIniDataParser();
            _iniConfigFile = parser.ReadData(reader);
        }

        public OptionsBuilder UseDefaults() => UseConfigName("defaults");

        public OptionsBuilder UseFromEnvironment()
        {
            var configName = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME");

            UseConfigName(configName);

            _profile = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE") ?? _profile;
            _username = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME") ?? _username;
            _password = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_PASSWORD") ?? _password;
            _otp = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_OTP") ?? _otp;
            _durationSeconds = Environment.GetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS") ?? _durationSeconds;

            return this;
        }

        public OptionsBuilder UseUsername(string? username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                _username = username;
            }

            return this;
        }

        public OptionsBuilder UseProfile(string? profile)
        {
            if (!string.IsNullOrEmpty(profile))
            {
                _profile = profile;
            }

            return this;
        }

        public OptionsBuilder UseRegion(string? region)
        {
            if (!string.IsNullOrEmpty(region))
            {
                _region = region;
            }

            return this;
        }

        public OptionsBuilder UseConfigName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return this;

            var data = _iniConfigFile[name];

            _baseUri = data["base_uri"] ?? _baseUri;
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

        public Options Build()
        {
            if (string.IsNullOrEmpty(_baseUri) || string.IsNullOrEmpty(_clientId) ||
                string.IsNullOrEmpty(_clientSecret) || string.IsNullOrEmpty(_subdomain) ||
                string.IsNullOrEmpty(_awsAppId)
            )
            {
                ThrowMissingRequiredSettingsException();
            }

            return new(
                BaseUri: _baseUri, Subdomain: _subdomain, ClientId: _clientId,
                ClientSecret: _clientSecret, DurationSeconds: _durationSeconds, AwsAppId: _awsAppId
            )
            {
                Username = _username,
                Password = _password,
                OTP = _otp,
                Profile = _profile,
                OTPDeviceId = _otpDeviceId,
                RoleARN = _roleARN,
                Region = _region,
            };
        }

        [DoesNotReturn]
        public void ThrowMissingRequiredSettingsException() =>
            throw new MissingRequiredSettingsException(
                baseUri: _baseUri,
                subdomain: _subdomain,
                clientId: _clientId,
                clientSecret: _clientSecret,
                profile: _profile,
                awsAppId: _awsAppId
            );
    }
}
