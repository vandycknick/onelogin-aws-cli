using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Amazon.Runtime;
using Amazon.SecurityToken.Model;
using IniParser;
using OneloginAwsCli.Api;
using OneloginAwsCli.Extensions;

namespace OneloginAwsCli
{
    static class Program
    {
        private const string CONFIG_FILE = ".onelogin-aws.config";
        private const string AWS_CONFIG_FILE = ".aws/credentials";

        static Task Main(string[] args)
        {
            var command = new RootCommand("OneLogin aws cli")
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
                    new string[] { "-v", "--verbose"}
                )
                {
                    Argument = new Argument<bool>(),
                    Description = "Verbose output.",
                }
            };

            command.Handler = CommandHandler.Create<string, bool, IConsole>(RootCommand);

            return command.InvokeAsync(args);
        }

        static async Task RootCommand(string profile, bool verbose, IConsole console)
        {
            console.WriteLine("Starting");
            var credentials = new Credentials();

            if (console.IsInputRedirected)
            {
                credentials = await ReadCredentialsFromInput();
            }
            else
            {
                throw new NotSupportedException("Only supports input via console in");
            }

            console.WriteLineIf(JsonSerializer.Serialize(credentials), verbose);

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

            var onelogin = new OneLoginClient(clientId, clientSecret);

            var saml = await onelogin.GenerateSamlAssertion(
                usernameOrEmail: credentials.Username,
                password: credentials.Password,
                appId: appId,
                subdomain: subdomain
            );
            console.WriteLineIf(JsonSerializer.Serialize(saml), verbose);

            var otp = credentials.OTP;
            if (string.IsNullOrEmpty(credentials.OTP))
            {
                Console.Write("Type in OTP: ");
                otp = Console.ReadLine();
            }

            var factor = await onelogin.VerifyFactor(
                appId: appId,
                deviceId: saml.Devices.FirstOrDefault().DeviceId,
                stateToken: saml.StateToken,
                otpToken: otp
            );

            byte[] data = Convert.FromBase64String(factor.Data);
            string decodedString = Encoding.UTF8.GetString(data);

            var roles = GetIAMRoleArns(decodedString);
            var index = 0;

            foreach (var role in roles)
            {
                index++;
                Console.WriteLine($"[{index}] {role.Role}");
            }

            console.Write("Make a choice: ");
            var choice = Console.ReadLine();
            var c = int.Parse(choice);

            var picked = roles.ElementAt(c - 1);

            if (picked.Role == null) throw new Exception("Invalid choice");


            var stsClient1 = new Amazon.SecurityToken.AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials());

            var assumeRoleReq = new AssumeRoleWithSAMLRequest
            {
                DurationSeconds = int.Parse(duration),
                RoleArn = picked.Role,
                PrincipalArn = picked.Principal,
                SAMLAssertion = factor.Data
            };

            var assumeRoleRes = await stsClient1.AssumeRoleWithSAMLAsync(assumeRoleReq);
            console.WriteLineIf(JsonSerializer.Serialize(assumeRoleRes), verbose);

            var awsConfig = parser.ReadFile(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE)
            );

            awsConfig[profile]["aws_access_key_id"] = assumeRoleRes.Credentials.AccessKeyId;
            awsConfig[profile]["aws_secret_access_key"] = assumeRoleRes.Credentials.SecretAccessKey;
            awsConfig[profile]["aws_session_token"] = assumeRoleRes.Credentials.SessionToken;

            parser.WriteFile(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AWS_CONFIG_FILE),
                parsedData: awsConfig,
                fileEncoding: Encoding.UTF8
            );

            console.WriteLine("Finished");
        }

        static List<(string Role, string Principal)> GetIAMRoleArns(string saml)
        {
            var roles = new List<(string Role, string Principal)>();
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
                var role = (Role: parts[0], Principal: parts[1]);
                roles.Add(role);
            }

            return roles;
        }
        static async Task<Credentials> ReadCredentialsFromInput()
        {
            using var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding);
            var input = await sr.ReadLineAsync();

            var creds = JsonSerializer.Deserialize<Credentials>(input, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return creds;
        }
    }
}
