#!/usr/bin/env bash
# Linux counterpart of build-android.ps1. Usage: build-android.sh [--debug] [--signing-file FILE]
set -euo pipefail
cd "$(dirname "$0")/.."

debug_build=false
signing_file=''
while [[ $# -gt 0 ]]; do
    case "$1" in
        --debug) debug_build=true; shift ;;
        --signing-file) signing_file="$2"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if ! $debug_build && [[ -z "$signing_file" && -z "${TLP_KEYSTORE:-}" ]]; then
    signing_file='signing/signing.json'
fi
if [[ -n "$signing_file" ]]; then
    signing_file="$(realpath "$signing_file")"
    read_json() { node -e "process.stdout.write(require(process.argv[1])[process.argv[2]])" "$signing_file" "$1"; }
    keystore="$(read_json keystore)"
    [[ "$keystore" = /* ]] || keystore="$(dirname "$signing_file")/$keystore"
    export TLP_KEYSTORE="$keystore"
    export TLP_STORE_PASSWORD="$(read_json password)"
    export TLP_KEY_ALIAS="$(read_json alias)"
fi
if ! $debug_build && [[ -z "${TLP_KEYSTORE:-}" ]]; then
    echo 'Supply signing/signing.json, TLP_KEYSTORE and TLP_STORE_PASSWORD, or --signing-file.' >&2; exit 1
fi
[[ -x "${JAVA_HOME:-}/bin/javac" ]] || { echo 'JAVA_HOME must point to a JDK 21 installation.' >&2; exit 1; }
[[ -n "${ANDROID_HOME:-}" ]] || { echo 'Set ANDROID_HOME to the Android SDK directory (platform 36 and build tools 36.0.0).' >&2; exit 1; }

npm ci
npm test
npm run typecheck
npm run android:sync

task=$($debug_build && echo ':app:assembleDebug' || echo ':app:assembleRelease')
(cd android && ./gradlew "$task" --console=plain)
