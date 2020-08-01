#!/bin/bash
set -o pipefail -o noclobber -o nounset

PROFILE=""
ITEM='<1Password Login Item should go here>'

function show_help {
    echo """aws-login:
  OneLogin aws cli

Usage:
  aws-login [options]

Options:
  -p <profile> (REQUIRED)               AWS profile to use.
  -?, -h                                Show help and usage information"""
}

function login {
    if [ -z "$PROFILE" ]; then
        echo "Profile is required"
        exit 1
    fi

    DATA=$(op get item "$ITEM" 2>/dev/null)

    if [ -z "$DATA" ]; then
        echo "Please login to 1password: eval \$(op signin <subdomain>)"
        exit 1
    fi

    USERNAME=$(echo $DATA | jq '.details.fields[] | select(.name == "email") | .value')
    PASSWORD=$(echo $DATA | jq '.details.fields[] | select(.name == "password") | .value')

    OTP=$(op get totp "$ITEM")

    CREDS="{ \"username\": $USERNAME, \"password\": $PASSWORD, \"otp\": \"$OTP\" }"

    OS=$(uname -s | awk '{print tolower($0)}' | sed "s/darwin/osx/")
    EXE="./artifacts/$OS-x64/onelogin-aws"

    (echo $CREDS && cat) | $EXE --profile $PROFILE
}

while getopts "h?p:" opt; do
    case "$opt" in
    h|\?)
        show_help
        exit 0
        ;;
    p)
        PROFILE=$OPTARG
        ;;
    esac
done

if [ -n "$PROFILE" ]; then
    login
    exit 0
else
    show_help
fi
