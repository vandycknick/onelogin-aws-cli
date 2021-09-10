# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v0.2.0] - 2021-09-10

### Added

-   Search for config file as per XDG spec
-   Check AWS cli env var AWS_CONFIG_FILE to locate aws config file before defaulting to ~/.aws/config

### Changed

-   Improved UI by adopting Spectre.Console instead of homegrown ansi parser/writer

### Removed

-   Remove piped json parsing and just result to env vars. This is a breaking change, which means it's no longer possible to pipe json into onelogin-aws.

## Fixed

-   Pass otp_device_id through from the config

## [v0.1.1] - 2020-08-27

### Fixed

-   Use RoleArn from config when this is provided instead of always asking for which role to use.

## [v0.1.0] - 2020-08-20

Initial release
