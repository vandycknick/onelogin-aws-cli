using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Moq;
using OneLoginAws.Exceptions;
using OneLoginAws.Services;
using Xunit;

namespace OneLoginAws.Test.Services
{
    public class SettingsBuilderTests
    {
        [Fact]
        public void SettingsBuilder_Ctor_ThrowsAConfigFileNotFoundExceptionWhenTheConfigFileIsNotFound()
        {
            // Given
            var mockFileSystem = new MockFileSystem();

            // When
            var exception = Assert.Throws<ConfigFileNotFoundException>(() => new SettingsBuilder(mockFileSystem.Object));

            // Then
            Assert.Equal(
                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config"),
                exception.FilePath
            );
        }

        [Fact]
        public void SettingsBuilder_UseDefaults_AddsValuesDefinedInDefaultsSection()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .Build();

            // Then
            Assert.Equal("123", settings.ClientId);
            Assert.Equal("456", settings.ClientSecret);
            Assert.Equal("the-default-one", settings.Profile);
            Assert.Equal("ddos", settings.Subdomain);
            Assert.Equal("789", settings.AwsAppId);
            Assert.Equal("43200", settings.DurationSeconds);
            Assert.Equal("us-east-1", settings.Region);
        }

        [Fact]
        public void SettingsBuilder_UseConfigName_AddsValuesDefinedInTheGivenSection()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseConfigName("my-profile")
                .Build();

            //T hen
            Assert.Equal("123", settings.ClientId);
            Assert.Equal("456", settings.ClientSecret);
            Assert.Equal("my-profile", settings.Profile);
            Assert.Equal("ddos", settings.Subdomain);
            Assert.Equal("987", settings.AwsAppId);
            Assert.Equal("666", settings.DurationSeconds);
            Assert.Equal("us-east-1", settings.Region);
        }

        [Fact]
        public void SettingsBuilder_UseEnvironment_AddsValuesFromConfigNameDefinedInEnvironment()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME", "my-profile-env");

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseFromEnvironment()
                .Build();

            // Then
            Assert.Equal("123", settings.ClientId);
            Assert.Equal("456", settings.ClientSecret);
            Assert.Equal("my-profile-env", settings.Profile);
            Assert.Equal("ddos", settings.Subdomain);
            Assert.Equal("5829", settings.AwsAppId);
            Assert.Equal("1038", settings.DurationSeconds);
            Assert.Equal("us-east-1", settings.Region);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME", null);
        }

        [Fact]
        public void SettingsBuilder_UseEnvironment_AddsValuesDefinedInEnvironment()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE", "from-env");
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME", "username-from-env");
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS", "2345");

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseFromEnvironment()
                .Build();

            // Then
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE", null);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME", null);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS", null);

            Assert.Equal("123", settings.ClientId);
            Assert.Equal("456", settings.ClientSecret);
            Assert.Equal("from-env", settings.Profile);
            Assert.Equal("username-from-env", settings.Username);
            Assert.Equal("ddos", settings.Subdomain);
            Assert.Equal("789", settings.AwsAppId);
            Assert.Equal("2345", settings.DurationSeconds);
            Assert.Equal("us-east-1", settings.Region);
        }

        [Fact]
        public void SettingsBuilder_UseEnvironment_AddsValuesFromEnvVarsTakePriorityOverProfileNameEnvVar()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME", "my-profile-env");
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE", "from-env");
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME", "username-from-env");
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS", "2345");

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseFromEnvironment()
                .Build();

            // Then
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_CONFIG_NAME", null);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_PROFILE", null);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_USERNAME", null);
            Environment.SetEnvironmentVariable("ONELOGIN_AWS_CLI_DURATION_SECONDS", null);

            Assert.Equal("123", settings.ClientId);
            Assert.Equal("456", settings.ClientSecret);
            Assert.Equal("from-env", settings.Profile);
            Assert.Equal("username-from-env", settings.Username);
            Assert.Equal("ddos", settings.Subdomain);
            Assert.Equal("5829", settings.AwsAppId);
            Assert.Equal("2345", settings.DurationSeconds);
            Assert.Equal("us-east-1", settings.Region);
        }

        [Fact]
        public void SettingsBuilder_UseUsername_AddsGivenUserName()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseUsername("other-username")
                .Build();

            // Then
            Assert.Equal("other-username", settings.Username);
        }

        [Fact]
        public void SettingsBuilder_UseProfile_AddsGivenProfile()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseProfile("other-profile")
                .Build();

            // Then
            Assert.Equal("other-profile", settings.Profile);
        }

        [Fact]
        public void SettingsBuilder_UseRegion_AddsGivenRegion()
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseRegion("other-region")
                .Build();

            // Then
            Assert.Equal("other-region", settings.Region);
        }

        [Theory]
        [MemberData(nameof(GetJsonStrings))]
        public void SettingsBuilder_UseFromJsonInput_AddsValuesFromJsonString(string json, Dictionary<string, string> expected)
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, TestFileUno);

            // When
            var settings = new SettingsBuilder(mockFileSystem.Object)
                .UseDefaults()
                .UseFromJson(json)
                .Build();

            // Then
            foreach (var record in expected)
            {
                var value = settings.GetType().GetProperty(record.Key).GetValue(settings, null);
                Assert.Equal(record.Value, value);
            }
        }

        [Theory]
        [MemberData(nameof(GetMissingRequiredSettingTestData))]
        public void SettingsBuilder_Build_ThrowsAnExceptionWhenARequiredFieldIsNotSet(string file, Dictionary<string, bool> expected)
        {
            // Given
            var fileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(fileName, file);

            // When
            var exception = Assert.Throws<MissingRequiredSettingsException>(() =>
            {
                var settings = new SettingsBuilder(mockFileSystem.Object)
                    .UseDefaults()
                    .Build();
            });

            // Then
            foreach (var prop in expected)
            {
                var value = (string)exception.GetType().GetProperty(prop.Key).GetValue(exception, null);
                // The value should not be null or empty, in the test data we use True to indicate if a value is present or not.
                Assert.True(prop.Value == !string.IsNullOrEmpty(value), $"Expected {prop.Value}, Given: {value}.");
            }
        }

        public static IEnumerable<object[]> GetJsonStrings() =>
              new List<object[]>
            {
                new object[]
                {
                    "{\"username\": \"frank\"}",
                    new Dictionary<string, string>
                    {
                        { "Username", "frank" },
                    }
                },
                new object[]
                {
                    "{\"password\": \"secret\"}",
                    new Dictionary<string, string>
                    {
                        { "Password", "secret" },
                    }
                },
                new object[]
                {
                    "{\"otp\": \"123456\"}",
                    new Dictionary<string, string>
                    {
                        { "OTP", "123456" },
                    }
                },
                new object[]
                {
                    "{\"username\": \"frank\", \"password\": \"yolo\"}",
                    new Dictionary<string, string>
                    {
                        { "Username", "frank" },
                        { "Password", "yolo" },
                    }
                },
                new object[]
                {
                    "{\"username\": \"frank\", \"otp\": \"1234\"}",
                    new Dictionary<string, string>
                    {
                        { "Username", "frank" },
                        { "OTP", "1234" },
                    }
                },
                new object[]
                {
                    "{\"username\": \"frank\", \"password\": \"yolo\", \"otp\": \"1234\"}",
                    new Dictionary<string, string>
                    {
                        { "Username", "frank" },
                        { "Password", "yolo" },
                        { "OTP", "1234" },
                    }
                },
            };

        public static IEnumerable<object[]> GetMissingRequiredSettingTestData()
        {
            var records = Enumerable
                .Range(0, 1 << 6)
                .ToDictionary(key => key,
                 key => new Dictionary<string, bool>
                 {
                    { "BaseUri", (key >> 5 & 1) != 0 },
                    { "ClientId", (key >> 4 & 1) != 0 },
                    { "ClientSecret", (key >> 3 & 1) != 0 },
                    { "Subdomain", (key >> 2 & 1) != 0 },
                    { "AwsAppId", (key >> 1 & 1) != 0 },
                    { "Profile", (key >> 0 & 1) != 0 },
                 });

            static string PropToSettingsKey(string prop) =>
                prop switch
                {
                    "BaseUri" => "base_uri",
                    "ClientId" => "client_id",
                    "ClientSecret" => "client_secret",
                    "Subdomain" => "subdomain",
                    "AwsAppId" => "aws_app_id",
                    "Profile" => "profile",
                    _ => throw new NotImplementedException(),
                };

            foreach (var record in records)
            {
                if (record.Value.Values.Contains(false))
                {
                    var file = "[defaults]\n";
                    foreach (var props in record.Value)
                    {
                        if (props.Value)
                        {
                            file += $"{PropToSettingsKey(props.Key)} = {RandomString(5)}\n";
                        }
                    }

                    yield return new object[] { file, record.Value };
                }
            }
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        private const string TestFileUno = @"
[defaults]
base_uri = https://api.us.onelogin.com/
client_id = 123
client_secret = 456
subdomain = ddos
renew_seconds = 60
aws_app_id = 789
duration_seconds = 43200
region = us-east-1
profile = the-default-one

[my-profile]
profile = my-profile
aws_app_id = 987
duration_seconds = 666

[my-profile-env]
profile = my-profile-env
aws_app_id = 5829
duration_seconds = 1038
";

        private class MockFileSystem : Mock<IFileSystem>
        {
            private readonly Mock<IFileInfoFactory> _fileInfoFactoryMock;

            public MockFileSystem()
            {
                _fileInfoFactoryMock = new Mock<IFileInfoFactory>();

                var fileInfo = new Mock<IFileInfo>();
                fileInfo.Setup(f => f.Exists).Returns(false);
                _fileInfoFactoryMock.Setup(f => f.FromFileName(It.IsAny<string>())).Returns(fileInfo.Object);

                Setup(f => f.FileInfo).Returns(_fileInfoFactoryMock.Object);
            }

            public Mock<IFileInfo> AddFile(string name, string contents)
            {
                var fileInfo = new Mock<IFileInfo>();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

                fileInfo.Setup(f => f.Exists).Returns(true);
                fileInfo.Setup(f => f.Name).Returns(name);
                fileInfo.Setup(f => f.OpenText()).Returns(() => new StreamReader(stream));

                _fileInfoFactoryMock.Setup(f => f.FromFileName(name)).Returns(fileInfo.Object);

                return fileInfo;
            }
        }
    }
}
