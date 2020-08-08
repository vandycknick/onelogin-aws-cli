using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IniParser;
using OneloginAwsCli.Api;
using OneloginAwsCli.Console;
using OneloginAwsCli.Extensions;
using OneloginAwsCli.Models;
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
                    new string[] { "-p", "--profile"}
                )
                {
                    Argument = new Argument<string>()
                    {
                        Name = "profile",
                    },
                    Description = "AWS profile to use.",
                    IsRequired = true,
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
                    new string[] { "-v", "--verbose"}
                )
                {
                    Argument = new Argument<bool>(),
                    Description = "Verbose output.",
                }
            };

            command.Handler = CommandHandler.Create<string, string, bool>((profile, username, verbose) =>
            {
                var client = new OneLoginClient(new HttpClient());
                var handler = new LoginCommand(client, new SystemConsole());

                return handler.InvokeAsync(profile, username, verbose);
            });

            return command;
        }

        private const string CONFIG_FILE = ".onelogin-aws.config";
        private const string AWS_CONFIG_FILE = ".aws/credentials";
        private static Encoding s_utf8WithoutBom = new UTF8Encoding(false);

        private readonly IOneLoginClient _client;
        private readonly IConsole _console;
        private readonly AnsiStringBuilder _ansiBuilder;

        private readonly string _info;
        private readonly string _success;
        private readonly string _warning;
        private readonly string _error;
        private readonly string _question;
        private readonly string _pointer;

        public LoginCommand(IOneLoginClient client, IConsole console)
        {
            _client = client;
            _console = console;

            _ansiBuilder = new AnsiStringBuilder();
            _info = _ansiBuilder.Clear().Blue("ℹ").ToString();
            _success = _ansiBuilder.Clear().Green("✔").ToString();
            _warning = _ansiBuilder.Clear().Yellow("⚠").ToString();
            _error = _ansiBuilder.Clear().Red("✖").ToString();
            _question = _ansiBuilder.Clear().Green("?").ToString();
            _pointer = _ansiBuilder.Clear().Write("\x1b[38;5;245m").Write("❯").ResetColor().ToString();
        }

        private class IAMRole
        {
            public string Role { get; set; }
            public string Principal { get; set; }
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

        private void ReadCredentialsFromInput(ref Credentials credentials)
        {
            var line = _console.In.ReadLine();

            var creds = JsonSerializer.Deserialize<Credentials>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            credentials.Username = creds.Username ?? credentials.Username;
            credentials.Password = creds.Password;
            credentials.OTP = creds.OTP;
        }

        public Device SelectOTPDevice(SAMLResponse saml)
        {
            if (saml.Devices.Count == 1) return saml.Devices.FirstOrDefault();

            _console.Write("  ");
            var device = _console.Select(
                message: "Select your OTP Device:",
                items: saml.Devices,
                onRenderItem: (device, selected) => device.DeviceType,
                indent: 5
            );

            _console.WriteLine(
                _ansiBuilder.Clear().EraseLines(2)
                .Write($"  {_success} Select your OTP Device: ").Cyan(device.DeviceType)
                .ToString()
            );
            return device;
        }

        public async Task InvokeAsync(string profile, string username, bool verbose)
        {
            var credentials = new Credentials
            {
                Username = username,
            };

            if (_console.IsInputRedirected) ReadCredentialsFromInput(ref credentials);

            if (string.IsNullOrEmpty(credentials.Username))
            {
                credentials.Username = _console.Input<string>("Onelogin Username:");
                _console.Write(_ansiBuilder.Clear().EraseLines(2).ToString());
            }

            _console.WriteLine(
                _ansiBuilder
                    .Clear()
                    .Write($"{_success} Onelogin Username: ")
                    .Cyan(credentials.Username)
                    .ToString()
            );

            if (string.IsNullOrEmpty(credentials.Password))
            {
                credentials.Password = _console.Password("Onelogin Password:");
                _console.Write(_ansiBuilder.Clear().EraseLines(2).ToString());
            }

            _console.WriteLine(
                _ansiBuilder
                    .Clear()
                    .Write($"{_success} Onelogin Password: ")
                    .Cyan("[input is masked]")
                    .ToString()
            );

            var parser = new FileIniDataParser();
            var config = parser.ReadFile(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), CONFIG_FILE)
            );

            // TODO: validate these values
            var clientId = config["defaults"]["client_id"];
            var clientSecret = config["defaults"]["client_secret"];
            var subdomain = config["defaults"]["subdomain"];
            var appId = config["defaults"]["aws_app_id"];
            var duration = config["defaults"]["duration_seconds"];

            _client.Credentials = new OneLoginCredentials
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            var saml = string.Empty;
            var typedOTP = credentials.OTP == null;
            using (var spinner = _console.RenderSpinner(true))
            {
                _console.Write("  Requesting SAML assertion");
                var samlResponse = await _client.GenerateSamlAssertion(
                    usernameOrEmail: credentials.Username,
                    password: credentials.Password,
                    appId: appId,
                    subdomain: subdomain
                );

                saml = samlResponse.Data;
                if (samlResponse.Message != "Success")
                {
                    if (string.IsNullOrEmpty(credentials.OTP) || samlResponse.Devices.Count > 1)
                    {
                        spinner.Stop();
                        _console.WriteLine(_ansiBuilder.Clear().CursorLeft().Write($"{_warning} Requesting SAML assertion").ToString());
                    }

                    var device = SelectOTPDevice(samlResponse);

                    if (string.IsNullOrEmpty(credentials.OTP))
                    {
                        _console.Write($"  ");
                        credentials.OTP = _console.Input<string>("OTP Token:");
                    }

                    var factor = await _client.VerifyFactor(
                        appId: appId,
                        deviceId: device.DeviceId,
                        stateToken: samlResponse.StateToken,
                        otpToken: credentials.OTP
                    );

                    saml = factor.Data;
                }
            }

            _console.Write(_ansiBuilder.EraseLines(typedOTP ? 3 : 2).CursorLeft().ToString());
            _console.WriteLine($"{_success} Requesting SAML assertion");
            if (typedOTP) _console.WriteLine($"  {_success} OTP Token: {credentials.OTP}");

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
                    DurationSeconds = int.Parse(duration),
                    RoleArn = role.Role,
                    PrincipalArn = role.Principal,
                    SAMLAssertion = saml
                };

                var assumeRoleRes = await stsClient.AssumeRoleWithSAMLAsync(assumeRoleReq);
                // console.WriteLineIf(() => JsonSerializer.Serialize(assumeRoleRes), verbose);
                expires = assumeRoleRes.Credentials.Expiration;

                var awsConfig = parser.ReadFile(
                    filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE)
                );

                awsConfig[profile]["aws_access_key_id"] = assumeRoleRes.Credentials.AccessKeyId;
                awsConfig[profile]["aws_secret_access_key"] = assumeRoleRes.Credentials.SecretAccessKey;
                awsConfig[profile]["aws_session_token"] = assumeRoleRes.Credentials.SessionToken;

                parser.WriteFile(
                    filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE),
                    parsedData: awsConfig,
                    fileEncoding: s_utf8WithoutBom
                );
            }

            _console.WriteLine($"{_success} Saving credentials:");
            _console.WriteLine($"  {_pointer} Credentials cached in '{Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE)}'");
            _console.WriteLine($"  {_pointer} Expires at {expires.ToLocalTime():yyyy-MM-dd H:mm:sszzz}");
            _console.WriteLine($"  {_pointer} Use aws cli with --profile {profile}");
        }
    }
}
