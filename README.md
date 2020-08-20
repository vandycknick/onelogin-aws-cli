# Onelogin Aws

[![Build status][ci-badge]][ci-url]
[![NuGet][nuget-package-badge]][nuget-package-url]
[![feedz.io][feedz-package-badge]][feedz-package-url]

> A CLI tool that simplifies using the AWS CLI in conjunction with OneLogin authentication.

## Introduction

A CLI utility to combine you AWS CLI with AWS Roles and OneLogin authentication. This is a rewrite of another tool originally written in [python](https://github.com/physera/onelogin-aws-cli). It tries to be mostly compatible with all the settings exposed in the original tool while adding some extra goodies to make your life easier. One of the current tool does not and never will support is built in keychain support. The preferred way is to pass any credentials from your password manager cli into the `onelogin-aws` tool or use the `security` cli on mac to grab a password from your keychain.

## Installation

```sh
dotnet tool install -g onelogin-aws
```

### Native
```sh
curl -L -o onelogin-aws{.exe} https://github.com/nickvdyck/onelogin-aws-cli/releases/download/{VERSION}/onelogin-aws.{PLATFORM}
```
You can replace the version portion by one of the releases found on GitHub.
The platform can be replaced with a supported [dotnet RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog), this represents the platform you want the tool to run on. The following ones are supported:
- osx-x64
- linux-x64
- win-x64 (not supported yet)

## Usage

```sh
λ onelogin-aws --help

onelogin-aws:
  OneLogin AWS cli

Usage:
  onelogin-aws [options] [command]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  login
  configure
```

Running `onelogin-aws login` will perform the authentication against OneLogin, and cache the credentials in the AWS CLI Shared Credentials File.

The tool is interactive and for missing piece of information, the program will present interactive inputs.

Settings can be provided via the following:
- [command line parameters](#command-line-parameters)
- [environment variables](#environment-variables)
- [piping secrets](#piping-secrets)
- [configuration file directives](#configuration-file)

### Command Line Parameters

- `-c`, `--config-name` - Config section to use.
- `--profile` - See the corresponding directive in the [configuration file](#configuration-file)
- `-u`, `--username` - See the corresponding directive in the [configuration file](#configuration-file)
- `-d`, `--duration-seconds` - See the corresponding directive in the [configuration file](#configuration-file)
- `-r`, `--region` - See the corresponding directive in the [configuration file](#configuration-file)

### Environment Variables

- `AWS_SHARED_CREDENTIALS_FILE` - Location of the AWS credentials file to write credentials to.
  See [AWS CLI Environment Variables][aws-cli-environment-variables] for more information.
- `ONELOGIN_AWS_CLI_CONFIG_NAME` - Config section to use.
- `ONELOGIN_AWS_CLI_PROFILE` - See the corresponding directive in the [configuration file](#configuration-file).
- `ONELOGIN_AWS_CLI_USERNAME` - See the corresponding directive in the [configuration file](#configuration-file).
- `ONELOGIN_AWS_CLI_DURATION_SECONDS` - See the corresponding directive in the [configuration file](#configuration-file).

### Piping Secrets
The tool doesn't have any native integration with keychain or any other password managers. It's preferred to pipe in secrets from another cli tool.

There are 2 ways you can pipe along secrets:
1. By constructing a json structure as a single line. When given an invalid json structure the tool will fall back to reading configuration from stdin.
```sh
echo "{ \"username\": \"username\", \"password\": \"password\", \"otp\": \"otp\" }" | onelogin-aws login -p some-profile
```

2. By piping in each cli input seperated by a newline
```sh
echo -e "username\npassword\notp\n2\n" | onelogin-aws login -p some-profile
```
The values piped in are username, password, otp token, iam role.

It's preferred to wrap this up in a shell script to make sure you do not accidentally expose any secret to your bash history. Have a look at `aws-login.sh` for an example on how to combine the tool with the 1password cli.

## Configuration File

The configuration file is located at `~/.onelogin-aws.config`.

It is an `.ini` file where each section defines a config name,
which can be provided using either the command line parameter `--config-name`
or the environment variable `ONELOGIN_AWS_CLI_CONFIG_NAME`.

If no config name is provided, the `[defaults]` section is used automatically.

All other sections automatically inherit from the `[defaults]` section,
and can define any additional directives as desired.

### Directives

- `base_uri` - OneLogin API base URI.
  One of either `https://api.us.onelogin.com/`,
  or `https://api.eu.onelogin.com/` depending on your OneLogin account.
- `subdomain` - The subdomain you authenticate against in OneLogin.
  This will be the first part of your onelogin domain.
  Eg, In `http://my_company.onelogin.com`, `my_company` would be the subdomain.
- `username` - Username to be used to authenticate against OneLogin with.
  Can also be set with the environment variable `ONELOGIN_AWS_CLI_USERNAME`.
- `client_id` - Client ID for the user to use to authenticate against the
  OneLogin api.
  See [Working with API Credentials][onelogin-working-with-api-credentials]
  for more details.
- `client_secret` - Client Secret for the user to use to authenticate against
  the OneLogin api.
  See [Working with API Credentials][onelogin-working-with-api-credentials]
  for more details.
- `save_password` - Flag indicating whether `onlogin-aws-cli` can save the
  onelogin password to an OS keychain.
  This functionality supports all keychains supported by
  [keyring][keyring-pypi].
- `profile` - AWS CLI profile to store credentials in.
  This refers to an AWS CLI profile name defined in your `~/.aws/config` file.
- `duration_seconds` - Length of the IAM STS session in seconds.
  This cannot exceed the maximum duration specified in AWS for the given role.
- `aws_app_id` - ID of the AWS App instance in your OneLogin account.
  This ID can be found by logging in to your OneLogin web dashboard
  and navigating to `Administration` -> `APPS` -> `<Your app instance>`,
  and copying it from the URL in the address bar.
- `role_arn` - AWS Role ARN to assume after authenticating against OneLogin.
  Specifying this will disable the display of available roles and the
  interactive choice to select a role after authenticating.
- `otp_device` - Allow the automatic selection of an OTP device.
  This value is the human readable string name for the device.
  Eg, `OneLogin Protect`, `Yubico YubiKey`, etc
- `ip_address` - The client IP address to send to OneLogin.
  Relevant when using OneLogin Policies with an IP whitelist.
  If this is specified, `auto_determine_ip_address` is not used.
- `auto_determine_ip_address` - Automatically determine the client IP address.
  Relevant when using OneLogin Policies with an IP whitelist.
  Can be used without specifying `ip_address`.

### Example

```ini
[defaults]
base_uri = https://api.us.onelogin.com/
subdomain = mycompany
username = john@mycompany.com
client_id = f99ee51f00400649280db1028ffa3ca9b21b680f2189b238d342cc8158c401c7
client_secret = a85234b6db01a29a493e2422d7930dffe6f4d3a826270a18838574f6b8ef7c3e
save_password = yes
profile = mycompany-onelogin
duration_seconds = 3600

[testing]
aws_app_id = 555029

[staging]
aws_app_id = 555045

[live]
aws_app_id = 555070

[testing-admin]
aws_app_id = 555029
role_arn = arn:aws:iam::123456789123:role/Admin

[staging-admin]
aws_app_id = 555045
role_arn = arn:aws:iam::123456789123:role/Admin

[live-admin]
aws_app_id = 555070
role_arn = arn:aws:iam::123456789123:role/Admin
```

This example will let you select from 6 config names, that are variations of the same base values specified in `[defaults]`.

The first three, `testing`, `staging`, and `live`, all have different OneLogin application IDs.

The latter three, `testing-admin`, `staging-admin`, and `live-admin`, also have `role_arn` specified, so they will automatically assume the role with that ARN.

For example, to use the `staging` config, you could run:

```sh
$ onelogin-aws login -C staging
```

And to use the `live-admin` config, you could run:

```sh
$ onelogin-aws login -c live-admin
```

## License

Copyright 2020 [Nick Van Dyck](https://nvd.codes)

MIT

[ci-url]: https://github.com/nickvdyck/onelogin-aws-cli
[ci-badge]: https://github.com/nickvdyck/onelogin-aws-cli/workflows/Main/badge.svg

[nuget-package-url]: https://www.nuget.org/packages/onelogin-aws/
[nuget-package-badge]: https://img.shields.io/nuget/v/onelogin-aws.svg?style=flat-square&label=nuget

[feedz-package-url]: https://f.feedz.io/nvd/onelogin-aws-cli/packages/onelogin-aws/latest/download
[feedz-package-badge]: https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fnvd%2Fonelogin-aws-cli%2Fshield%2Fonelogin-aws%2Flatest&label=onelogin-aws
