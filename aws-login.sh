#!/bin/bash
set -o pipefail -o noclobber -o nounset

PROFILE=""
CONFIG_NAME=""
ITEM=""

function show_help {
    echo """aws-login:
  AWS Login CLI

Usage:
  aws-login [options]

Options:
  -p <profile>                          AWS profile to use.
  -c <config-name>                      Configuration to use.
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

    if [ ! -z "$PROFILE" ]; then
        ONELOGIN_AWS_CLI_USERNAME=$USERNAME ONELOGIN_AWS_CLI_PASSWORD=$PASSWORD ONELOGIN_AWS_CLI_OTP=$OTP onelogin-aws login --profile "$PROFILE"
    elif [ ! -z "$CONFIG_NAME" ]; then
        ONELOGIN_AWS_CLI_USERNAME=$USERNAME ONELOGIN_AWS_CLI_PASSWORD=$PASSWORD ONELOGIN_AWS_CLI_OTP=$OTP onelogin-aws login --config-name "$CONFIG_NAME"
    else
        ONELOGIN_AWS_CLI_USERNAME=$USERNAME ONELOGIN_AWS_CLI_PASSWORD=$PASSWORD ONELOGIN_AWS_CLI_OTP=$OTP onelogin-aws login
    fi
}

while getopts "h?c:p:" opt; do
    case "$opt" in
    h|\?)
        show_help
        exit 0
        ;;
    p)
        PROFILE=$OPTARG
        ;;
    c)
        CONFIG_NAME=$OPTARG
        ;;
    esac
done

login
exit 0
