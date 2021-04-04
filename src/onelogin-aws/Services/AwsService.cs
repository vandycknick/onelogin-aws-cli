using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IniParser;

namespace OneLoginAws.Services
{
    public class AwsService
    {
        private const string DEFAULT_AWS_CONFIG_FILE = ".aws/config";
        private const string AWS_CONFIG_FILE = "AWS_CONFIG_FILE";
        private static readonly Encoding _utf8WithoutBom = new UTF8Encoding(false);

        private readonly IFileSystem _fileSystem;
        private readonly AmazonSecurityTokenServiceClient _stsClient;
        private readonly FileIniDataParser _iniParser;

        public string ConfigFile
        {
            get => Environment.GetEnvironmentVariable(AWS_CONFIG_FILE) ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DEFAULT_AWS_CONFIG_FILE);
        }

        public AwsService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _stsClient = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials());
            _iniParser = new FileIniDataParser();
        }

        public async Task<DateTime> AssumeRole(string roleArn, string principalArn, string saml, int duration, string profile)
        {
            if (!_fileSystem.File.Exists(ConfigFile))
            {
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
                var file = _fileSystem.File.Create(ConfigFile);
                await file.DisposeAsync();
                await Chmod(ConfigFile, "600");
            }

            var assumeRoleReq = new AssumeRoleWithSAMLRequest
            {
                RoleArn = roleArn,
                PrincipalArn = principalArn,
                SAMLAssertion = saml,
                DurationSeconds = duration,
            };


            var assumeRoleRes = await _stsClient.AssumeRoleWithSAMLAsync(assumeRoleReq);
            var parser = new FileIniDataParser();
            var awsConfig = parser.ReadFile(ConfigFile);

            awsConfig[profile]["aws_access_key_id"] = assumeRoleRes.Credentials.AccessKeyId;
            awsConfig[profile]["aws_secret_access_key"] = assumeRoleRes.Credentials.SecretAccessKey;
            awsConfig[profile]["aws_session_token"] = assumeRoleRes.Credentials.SessionToken;

            parser.WriteFile(
                filePath: ConfigFile,
                parsedData: awsConfig,
                fileEncoding: _utf8WithoutBom
            );

            return assumeRoleRes.Credentials.Expiration;
        }

        private async Task<bool> Chmod(string filePath, string permissions = "700", bool recursive = false)
        {
            string cmd;
            if (recursive)
                cmd = $"chmod -R {permissions} {filePath}";
            else
                cmd = $"chmod {permissions} {filePath}";

            try
            {
                using (var proc = Process.Start("/bin/sh", $"-c \"{cmd}\""))
                {
                    await proc.WaitForExitAsync();
                    return proc.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
