using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Amazon;
using OneLoginApi.Models;
using OneLoginAws.Models;
using OneLoginAws.Services;
using OneLoginAws.Extensions;
using Spectre.Console;

namespace OneLoginAws
{
    public class LoginCommand
    {
        public static Command Create()
        {
            var command = new Command("login")
            {
                new Option(
                    new string[] { "-C", "-c", "--config-name"}
                )
                {
                    Argument = new Argument<string>()
                    {
                        Name = "configName",
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
                var client = new OneLoginClientFactory();
                var fileSystem = new FileSystem();
                var builder = new OptionsBuilder(fileSystem);
                var console = AnsiConsole.Create(new AnsiConsoleSettings());
                var aws = new AwsService(fileSystem);
                var handler = new LoginCommand(client, builder, console, aws);

                return handler.InvokeAsync(profile, username, configName, region);
            });

            return command;
        }

        private static readonly Encoding _utf8WithoutBom = new UTF8Encoding(false);

        private readonly IOneLoginClientFactory _oneLoginClientFactory;
        private readonly OptionsBuilder _appOptionsBuilder;
        private readonly IAnsiConsole _ansiConsole;
        private readonly AwsService _aws;

        private readonly string _info;
        private readonly string _success;
        private readonly string _warning;
        private readonly string _error;
        // private readonly string _question;
        private readonly string _pointer;

        public LoginCommand(IOneLoginClientFactory oneLoginClientFactory, OptionsBuilder appOptionsBuilder, IAnsiConsole console, AwsService aws)
        {
            _oneLoginClientFactory = oneLoginClientFactory;
            _appOptionsBuilder = appOptionsBuilder;
            _ansiConsole = console;
            _aws = aws;

            _info = "[blue]ℹ [/]";
            _success = "[green] ✔[/]";
            _warning = "[yellow]⚠[/]";
            _error = "[red]✖[/red]";
            _pointer = "[grey54]❯[/]";
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

            if (nodes == null) return roles;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (node != null)
                {
                    var parts = node.InnerText.Trim().Split(',');
                    var role = new IAMRole(
                        Role: parts[0],
                        Principal: parts[1]
                    );
                    roles.Add(role);
                }
            }

            return roles;
        }

        private bool TryGetFallbackProfile(string role, string username, [NotNullWhen(returnValue: true)] out string profile)
        {
            profile = "";

            if (Arn.TryParse(role, out var arn))
            {
                var roleName = arn.Resource.Split('/').ElementAtOrDefault(1);
                if (roleName != null)
                {
                    profile = $"{arn.AccountId}/{roleName}/{username}";
                    return true;
                }

                return false;
            }

            return false;
        }

        public Device SelectOTPDevice(SAMLResponse saml)
        {
            if (saml.Devices.Count == 1)
            {
                return saml.Devices[0];
            }

            var otp = new SelectionPrompt<Device>()
                .Title("Select your OTP Device:")
                .PageSize(10)
                .UseConverter(device => $"[{device.DeviceId}]: {device.DeviceType}")
                .AddChoices(saml.Devices);

            return _ansiConsole.Prompt(otp);
        }

        public async Task InvokeAsync(string? profile, string? username, string? configName, string? region)
        {
            string? password, otp, otpDeviceId;

            _appOptionsBuilder
                .UseDefaults()
                .UseFromEnvironment()
                .UseConfigName(configName)
                .UseUsername(username)
                .UseProfile(profile)
                .UseRegion(region);

            var appOptions = _appOptionsBuilder.Build();
            (username, password, otp, otpDeviceId) = appOptions;

            var apiRegion = appOptions.BaseUri.Split(".").ElementAt(1);
            var client = _oneLoginClientFactory.Create(appOptions.ClientId, appOptions.ClientSecret, apiRegion);

            if (string.IsNullOrEmpty(username))
            {
                username = _ansiConsole.Ask<string>($"{_info} OneLogin Username: ");
                _ansiConsole.CursorUp();
                _ansiConsole.EraseLine();
            }

            _ansiConsole.MarkupLine($"{_success} OneLogin Username: [teal]{username}[/]");

            if (string.IsNullOrEmpty(password))
            {
                password = _ansiConsole.Prompt(
                    new TextPrompt<string>($"{_info} OneLogin Password:")
                        .Secret()
                );
                _ansiConsole.CursorUp();
                _ansiConsole.EraseLine();
            }

            _ansiConsole.MarkupLine($"{_success} OneLogin Password: [teal]***[/]");

            var saml = string.Empty;

            var response = await AnsiConsole.Status()
                .StartAsync("Requesting SAML assertion", async ctx =>
                {
                    var samlResponse = await client.SAML.GenerateSamlAssertion(
                        usernameOrEmail: username,
                        password: password,
                        appId: appOptions.AwsAppId,
                        subdomain: appOptions.Subdomain
                    );

                    return samlResponse;
                });

            if (response.Message != "Success")
            {
                var device = SelectOTPDevice(response);

                if (string.IsNullOrEmpty(otp))
                {
                    otp = _ansiConsole.Ask<string>($"{_info} OTP Token:");
                    _ansiConsole.CursorUp();
                }

                _ansiConsole.MarkupLine($"{_success} OTP Token: [teal]{otp}[/]");

                var factor = await AnsiConsole.Status()
                    .StartAsync("Verifying OTP", async ctx =>
                    {

                        var factor = await client.SAML.VerifyFactor(
                            appId: appOptions.AwsAppId,
                            deviceId: device.DeviceId,
                            stateToken: response.StateToken,
                            otpToken: otp
                        );

                        return factor;
                    });
                saml = factor.Data;
            }
            else
            {
                saml = response.Data;
            }

            var data = Convert.FromBase64String(saml);
            var decodedString = Encoding.UTF8.GetString(data);

            var roles = GetIAMRoleArns(decodedString);
            var iamPrompt = new SelectionPrompt<IAMRole>()
                .Title($"{_info} Choose a role:")
                .PageSize(20)
                .UseConverter(role => role.Role)
                .AddChoices(roles);

            var iamRole = string.IsNullOrEmpty(appOptions.RoleARN) ?
                            _ansiConsole.Prompt(iamPrompt) :
                            roles.Where(role => role.Role == appOptions.RoleARN).FirstOrDefault();

            if (iamRole is null)
            {
                throw new Exception($"Invalid IAM role: {appOptions.RoleARN}.");
            }

            _ansiConsole.MarkupLine($"{_success} Choose a role: [teal]{iamRole.Role}[/]");

            var appProfile = appOptions.Profile;

            if (appProfile is null && !TryGetFallbackProfile(iamRole.Role, username, out appProfile))
            {
                throw new Exception($"Unknown exception: can't generate profile name for role {iamRole.Role} and username {username}.");
            }

            var expires = await AnsiConsole.Status()
                .StartAsync("Saving credentials", _ =>
                    _aws.AssumeRole(
                        iamRole.Role, iamRole.Principal,
                        saml, int.Parse(appOptions.DurationSeconds), appProfile)
                );

            _ansiConsole.MarkupLine($"{_success} Saving credentials:");
            _ansiConsole.MarkupLine($"  {_pointer} Credentials cached in '{_aws.CredentialsFile}'");
            _ansiConsole.MarkupLine($"  {_pointer} Expires at {expires.ToLocalTime():yyyy-MM-dd H:mm:sszzz}");
            _ansiConsole.MarkupLine($"  {_pointer} Use aws cli with --profile {appProfile}");
        }
    }

    static class LoginCommandValidators
    {
        public static Argument<T> ValidateConfigNames<T>(this Argument<T> arg)
        {
            arg.AddValidator(result =>
            {
                var fileSystem = new FileSystem();
                var value = result.GetValueOrDefault<string>();
                var sections = OptionsBuilder.GetConfigNames(fileSystem);

                if (value is null)
                {
                    return $"Empty config name!";
                }

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
