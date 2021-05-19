#!/bin/bash
set -o pipefail -o noclobber -o nounset

PROFILE=""
COMPILE=""
ITEM='OneLogin Datacamp'

function show_help {
    echo """aws-login:
  OneLogin aws cli

Usage:
  aws-login [options]

Options:
  -p <profile>                          AWS profile to use.
  -c                                    Compiles a new version of the tool first
  -?, -h                                Show help and usage information"""
}

function login {
    DATA=$(op get item "$ITEM" 2>/dev/null)

    if [ -z "$DATA" ]; then
        echo "Please login to 1password: eval \$(op signin <subdomain>)"
        exit 1
    fi

    USERNAME=$(echo $DATA | jq -r '.details.fields[] | select(.name == "email") | .value')
    PASSWORD=$(echo $DATA | jq -r '.details.fields[] | select(.name == "password") | .value')
    OTP=$(op get totp "$ITEM")

    if [ -z "$COMPILE" ]; then
        OS=$(uname -s | awk '{print tolower($0)}' | sed "s/darwin/osx/")
        EXE="./artifacts/$OS-x64/onelogin-aws"
    else
        EXE="dotnet run -p src/onelogin-aws --"
    fi

    if [ -z "$PROFILE" ]; then
        ONELOGIN_AWS_CLI_USERNAME=$USERNAME ONELOGIN_AWS_CLI_PASSWORD=$PASSWORD ONELOGIN_AWS_CLI_OTP=$OTP $EXE login
    else
        ONELOGIN_AWS_CLI_USERNAME=$USERNAME ONELOGIN_AWS_CLI_PASSWORD=$PASSWORD ONELOGIN_AWS_CLI_OTP=$OTP $EXE login --profile "$PROFILE"
    fi
}

while getopts "h?cp:" opt; do
    case "$opt" in
    h|\?)
        show_help
        exit 0
        ;;
    p)
        PROFILE=$OPTARG
        ;;
    c)
        COMPILE="yes"
        ;;
    esac
done

login
exit 0
