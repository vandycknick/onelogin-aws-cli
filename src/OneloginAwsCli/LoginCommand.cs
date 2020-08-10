using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IniParser;
using OneloginAwsCli.Api;
using OneloginAwsCli.Api.Exceptions;
using OneloginAwsCli.Api.Models;
using OneloginAwsCli.Console;
using OneloginAwsCli.Extensions;
using OneloginAwsCli.Models;
using OneloginAwsCli.Services;
using IConsole = OneloginAwsCli.Console.IConsole;

namespace OneloginAwsCli
{
    public class LoginCommand
    {
        public static Command Create()
        {
            var command = new Command("login")
            {
                new Option(
                    new string[] { "-C", "--config-name"}
                )
                {
                    Argument = new Argument<string>()
                    {
                        Name = "config_name",
                        Arity = ArgumentArity.ExactlyOne,
                    }.ValidateConfigNames(),
                    Description = " Switch configuration name within config file",
                },
                new Option(
                    new string[] { "-p", "--profile"}
                )
                {
                    Argument = new Argument<string>()
                    {
                        Name = "profile",
                    },
                    Description = "AWS profile name.",
                },
                new Option(
                    new string[] { "-u", "--username" }
                )
                {
                    Argument = new Argument<string>()
                    {
                        Name = "username",
                    },
                    Description = "AWS profile to use.",
                },
                new Option(
                    new string[] { "-r", "--region"}
                )
                {
                    Argument = new Argument<string>()
                    {
                       Name = "region",
                       Arity = ArgumentArity.ExactlyOne,
                    },
                    Description = "Specify default region for AWS profile being updated"
                },
                new Option(
                    new string[] { "-v", "--verbose"}
                )
                {
                    Argument = new Argument<bool>(),
                    Description = "Verbose output.",
                }
            };

            command.Handler = CommandHandler.Create<string, string, string, string>((profile, configName, region, username) =>
            {
                var client = new OneLoginClient(new HttpClient());
                var handler = new LoginCommand(client, new SystemConsole(), new SettingsBuilder());

                return handler.InvokeAsync(profile, username, configName, region);
            });

            return command;
        }

        private const string AWS_CONFIG_FILE = ".aws/credentials";
        private static Encoding _utf8WithoutBom = new UTF8Encoding(false);

        private readonly IOneLoginClient _client;
        private readonly IConsole _console;
        private readonly ISettingsBuilder _settingsBuilder;
        private readonly AnsiStringBuilder _ansiBuilder;

        private readonly string _info;
        private readonly string _success;
        private readonly string _warning;
        private readonly string _error;
        private readonly string _question;
        private readonly string _pointer;

        public LoginCommand(IOneLoginClient client, IConsole console, ISettingsBuilder settingsBuilder)
        {
            _client = client;
            _console = console;
            _settingsBuilder = settingsBuilder;

            _ansiBuilder = new AnsiStringBuilder();
            _info = _ansiBuilder.Clear().Blue("ℹ").ToString();
            _success = _ansiBuilder.Clear().Green("✔").ToString();
            _warning = _ansiBuilder.Clear().Yellow("⚠").ToString();
            _error = _ansiBuilder.Clear().Red("✖").ToString();
            _question = _ansiBuilder.Clear().Green("?").ToString();
            _pointer = _ansiBuilder.Clear().Write("\x1b[38;5;245m").Write("❯").ResetColor().ToString();
        }

        private List<IAMRole> GetIAMRoleArns(string saml)
        {
            var roles = new List<IAMRole>();
            var document = new XmlDocument();
            document.LoadXml(saml);

            var m = new XmlNamespaceManager(document.NameTable);
            m.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            m.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");

            var nodes = document.SelectNodes("/samlp:Response/saml:Assertion/saml:AttributeStatement/saml:Attribute[@Name='https://aws.amazon.com/SAML/Attributes/Role']/saml:AttributeValue", m);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var parts = node.InnerText.Trim().Split(',');
                var role = new IAMRole
                {
                    Role = parts[0],
                    Principal = parts[1],
                };
                roles.Add(role);
            }

            return roles;
        }

        public Device SelectOTPDevice(SAMLResponse saml)
        {
            if (saml.Devices.Count == 1) return saml.Devices.FirstOrDefault();

            _console.Write("  ");
            var device = _console.Select(
                message: "Select your OTP Device:",
                items: saml.Devices,
                onRenderItem: (device, selected) => $"[{device.DeviceId}]: {device.DeviceType}",
                indent: 5
            );

            _console.WriteLine(
                _ansiBuilder.Clear().EraseLines(2)
                .Write($"  {_success} Select your OTP Device: ").Cyan(device.DeviceType)
                .ToString()
            );
            return device;
        }

        public async Task InvokeAsync(string profile, string username, string configName, string region)
        {
            _settingsBuilder
                .UseDefaults()
                .UseFromEnvironment()
                .UseConfigName(configName)
                .UseCommandLineOverrides(profile, username, region);

            if (_console.IsInputRedirected)
            {
                _settingsBuilder.UseFromJsonInput(_console.In);
            }

            var settings = _settingsBuilder.Build();

            _client.Credentials = new OneLoginCredentials
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
            };

            if (string.IsNullOrEmpty(settings.Username))
            {
                settings.Username = _console.Input<string>("Onelogin Username:");
                _console.Write(_ansiBuilder.Clear().EraseLines(2).ToString());
            }

            _console.WriteLine(
                _ansiBuilder
                    .Clear()
                    .Write($"{_success} Onelogin Username: ")
                    .Cyan(settings.Username)
                    .ToString()
            );

            if (string.IsNullOrEmpty(settings.Password))
            {
                settings.Password = _console.Password("Onelogin Password:");
                _console.Write(_ansiBuilder.Clear().EraseLines(2).ToString());
            }

            _console.WriteLine(
                _ansiBuilder
                    .Clear()
                    .Write($"{_success} Onelogin Password: ")
                    .Cyan("[input is masked]")
                    .ToString()
            );

            var saml = string.Empty;
            var typedOTP = settings.OTP == null;

            try
            {
                using (var spinner = _console.RenderSpinner(true))
                {
                    _console.Write("  Requesting SAML assertion");
                    var samlResponse = await _client.GenerateSamlAssertion(
                        usernameOrEmail: settings.Username,
                        password: settings.Password,
                        appId: settings.AwsAppId,
                        subdomain: settings.Subdomain
                    );

                    saml = samlResponse.Data;
                    if (samlResponse.Message != "Success")
                    {
                        if (string.IsNullOrEmpty(settings.OTP) || samlResponse.Devices.Count > 1)
                        {
                            spinner.Stop();
                            _console.WriteLine(_ansiBuilder.Clear().CursorLeft().Write($"{_warning} Requesting SAML assertion").ToString());
                        }

                        var device = SelectOTPDevice(samlResponse);

                        if (string.IsNullOrEmpty(settings.OTP))
                        {
                            _console.Write($"  ");
                            settings.OTP = _console.Input<string>("OTP Token:");
                        }

                        var factor = await _client.VerifyFactor(
                            appId: settings.AwsAppId,
                            deviceId: device.DeviceId,
                            stateToken: samlResponse.StateToken,
                            otpToken: settings.OTP
                        );

                        saml = factor.Data;
                    }
                }

                _console.Write(_ansiBuilder.EraseLines(typedOTP ? 3 : 2).CursorLeft().ToString());
                _console.WriteLine($"{_success} Requesting SAML assertion");
                if (typedOTP) _console.WriteLine($"  {_success} OTP Token: {settings.OTP}");
            }
            catch (ApiException)
            {
                _console.Write(_ansiBuilder.EraseLines(1).CursorLeft().ToString());
                _console.WriteLine($"{_error} Requesting SAML assertion");
                throw;
            }

            var data = Convert.FromBase64String(saml);
            var decodedString = Encoding.UTF8.GetString(data);

            var roles = GetIAMRoleArns(decodedString);
            var role = _console.Select(
                message: "Choose a role:",
                items: roles,
                onRenderItem: (iam, selected) => iam.Role,
                indent: 2
            );

            _console.WriteLine(
                _ansiBuilder
                    .Clear()
                    .EraseLines(2)
                    .Write($"{_success} Choose a role: ")
                    .Cyan(role.Role)
                    .ToString()
            );

            var expires = DateTime.UtcNow;
            using (var spinner = _console.RenderSpinner(true))
            {
                _console.Write("  Saving credentials");

                var stsClient = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials());

                var assumeRoleReq = new AssumeRoleWithSAMLRequest
                {
                    DurationSeconds = int.Parse(settings.DurationSeconds),
                    RoleArn = role.Role,
                    PrincipalArn = role.Principal,
                    SAMLAssertion = saml
                };

                var assumeRoleRes = await stsClient.AssumeRoleWithSAMLAsync(assumeRoleReq);
                expires = assumeRoleRes.Credentials.Expiration;

                var parser = new FileIniDataParser();
                var awsConfig = parser.ReadFile(
                    filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE)
                );

                awsConfig[profile]["aws_access_key_id"] = assumeRoleRes.Credentials.AccessKeyId;
                awsConfig[profile]["aws_secret_access_key"] = assumeRoleRes.Credentials.SecretAccessKey;
                awsConfig[profile]["aws_session_token"] = assumeRoleRes.Credentials.SessionToken;

                parser.WriteFile(
                    filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE),
                    parsedData: awsConfig,
                    fileEncoding: _utf8WithoutBom
                );
            }

            _console.WriteLine($"{_success} Saving credentials:");
            _console.WriteLine($"  {_pointer} Credentials cached in '{Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE)}'");
            _console.WriteLine($"  {_pointer} Expires at {expires.ToLocalTime():yyyy-MM-dd H:mm:sszzz}");
            _console.WriteLine($"  {_pointer} Use aws cli with --profile {profile}");
        }
    }

    static class LoginCommandValidators
    {
        public static Argument<T> ValidateConfigNames<T>(this Argument<T> arg)
        {
            arg.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();
                var sections = SettingsBuilder.GetConfigNames();

                if (sections.Contains(value))
                {
                    return null;
                }

                return $"Given config name `{value}` does not exist in your config file.";
            });

            return arg;
        }
    }
}
