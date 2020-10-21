#!/bin/bash
set -o pipefail -o noclobber -o nounset

PROFILE=""
COMPILE=""
ITEM='<item name>'

function show_help {
    echo """aws-login:
  OneLogin aws cli

Usage:
  aws-login [options]

Options:
  -p <profile> (REQUIRED)               AWS profile to use.
  -n                                    Compiles a new version of the tool first
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

    USERNAME=$(echo $DATA | jq -r '.details.fields[] | select(.name == "email") | .value')
    PASSWORD=$(echo $DATA | jq -r '.details.fields[] | select(.name == "password") | .value')
    OTP=$(op get totp "$ITEM")

    if [ -z "$COMPILE" ]; then
        OS=$(uname -s | awk '{print tolower($0)}' | sed "s/darwin/osx/")
        EXE="./artifacts/$OS-x64/onelogin-aws"
    else
        EXE="dotnet run -p src/onelogin-aws --"
    fi

    (jo username=$USERNAME password=$PASSWORD -s otp=$OTP && cat) | \
        ($EXE login --profile $PROFILE && echo "Press Enter to exit.")
}

while getopts "h?bp:" opt; do
    case "$opt" in
    h|\?)
        show_help
        exit 0
        ;;
    p)
        PROFILE=$OPTARG
        ;;
    b)
        COMPILE="yes"
        ;;
    esac
done

if [ -n "$PROFILE" ]; then
    login
    exit 0
else
    show_help
fi
