#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
global_json="${repo_root}/global.json"
install_dir="${HOME}/.dotnet"
install=false

usage() {
    echo "Usage: bash scripts/ensure-dotnet-sdk.sh [--install] [--global-json PATH] [--install-dir PATH]"
}

fail() {
    echo "$1" >&2
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --install)
            install=true
            ;;
        --global-json)
            shift
            [[ $# -gt 0 ]] || fail "--global-json requires a path."
            global_json="$1"
            ;;
        --install-dir)
            shift
            [[ $# -gt 0 ]] || fail "--install-dir requires a path."
            install_dir="$1"
            ;;
        --help | -h)
            usage
            exit 0
            ;;
        *)
            usage >&2
            fail "Unknown option: $1"
            ;;
    esac
    shift
done

[[ -f "${global_json}" ]] || fail "global.json not found: ${global_json}"

command -v jq > /dev/null 2>&1 || fail "Reading SDK release metadata requires jq."

read_required_version() {
    jq -er '.sdk.version | select(type == "string" and length > 0)' "${global_json}"
}

required_version="$(read_required_version)"
version_pattern='^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$'
[[ "${required_version}" =~ ${version_pattern} ]] || fail "Invalid sdk.version in ${global_json}: ${required_version}"

sdk_is_installed() {
    local installed_sdks=""

    if [[ -f "${install_dir}/sdk/${required_version}/dotnet.dll" ]]; then
        return 0
    fi

    if command -v dotnet > /dev/null 2>&1; then
        installed_sdks="$(dotnet --list-sdks 2> /dev/null || true)"
        if grep -Eq "^${required_version//./\\.} \[" <<< "${installed_sdks}"; then
            return 0
        fi
    fi

    return 1
}

# The function intentionally reports availability through its status.
# shellcheck disable=SC2310
if sdk_is_installed; then
    echo ".NET SDK ${required_version} is already installed."
    exit 0
fi

if [[ "${install}" != true ]]; then
    echo ".NET SDK ${required_version} is required by ${global_json} but is not installed." >&2
    echo "Install it side-by-side without removing other SDKs:" >&2
    echo "  bash scripts/ensure-dotnet-sdk.sh --install --global-json \"${global_json}\" --install-dir \"${install_dir}\"" >&2
    exit 1
fi

command -v curl > /dev/null 2>&1 || fail "Installing the SDK requires curl."
command -v tar > /dev/null 2>&1 || fail "Installing the SDK requires tar."

detect_rid() {
    local os
    local os_name
    local architecture
    local machine_architecture

    os_name="$(uname -s)"
    case "${os_name}" in
        Linux)
            os="linux"
            if (ldd --version 2>&1 || true) | grep -qi musl; then
                os="linux-musl"
            fi
            ;;
        Darwin)
            os="osx"
            ;;
        *)
            fail "Unsupported operating system: ${os_name}"
            ;;
    esac

    machine_architecture="$(uname -m)"
    case "${machine_architecture}" in
        x86_64 | amd64)
            architecture="x64"
            ;;
        aarch64 | arm64)
            architecture="arm64"
            ;;
        armv7l | armv8l)
            architecture="arm"
            ;;
        *)
            fail "Unsupported architecture: ${machine_architecture}"
            ;;
    esac

    echo "${os}-${architecture}"
}

read_archive_record() {
    local metadata_file="$1"
    local rid="$2"

    jq -er --arg version "${required_version}" --arg rid "${rid}" '
        first(
            .releases[]
            | ((.sdks // []) + (if .sdk then [.sdk] else [] end))[]
            | select(.version == $version)
            | .files[]
            | select(.rid == $rid)
            | select((.name // "") | endswith(".tar.gz"))
            | [.url, .hash]
            | @tsv
        )
    ' "${metadata_file}"
}

channel="${required_version%.*}"
rid="$(detect_rid)"
metadata_url="https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/${channel}/releases.json"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT
metadata_file="${temp_dir}/releases.json"
archive_file="${temp_dir}/dotnet-sdk.tar.gz"
payload_dir="${temp_dir}/payload"

curl --fail --location --silent --show-error --output "${metadata_file}" "${metadata_url}"
set +e
archive_record="$(read_archive_record "${metadata_file}" "${rid}")"
archive_record_status=$?
set -e
[[ ${archive_record_status} -eq 0 ]] || fail "Official release metadata has no ${required_version} SDK archive for ${rid}."
IFS=$'\t' read -r archive_url expected_hash <<< "${archive_record}"

case "${archive_url}" in
    "https://builds.dotnet.microsoft.com/dotnet/Sdk/${required_version}/"*) ;;
    *) fail "Official release metadata returned an unexpected SDK URL: ${archive_url}" ;;
esac

[[ "${expected_hash}" =~ ^[0-9A-Fa-f]{128}$ ]] || fail "Official release metadata returned an invalid SHA-512 hash."
curl --fail --location --silent --show-error --output "${archive_file}" "${archive_url}"

if command -v sha512sum > /dev/null 2>&1; then
    hash_output="$(sha512sum "${archive_file}")"
elif command -v shasum > /dev/null 2>&1; then
    hash_output="$(shasum -a 512 "${archive_file}")"
else
    fail "SHA-512 verification requires sha512sum or shasum."
fi
actual_hash="${hash_output%% *}"

shopt -s nocasematch
if [[ "${actual_hash}" != "${expected_hash}" ]]; then
    fail "SHA-512 verification failed for .NET SDK ${required_version}."
fi
shopt -u nocasematch

mkdir -p "${payload_dir}"
if ! tar -xzf "${archive_file}" -C "${payload_dir}"; then
    fail "Failed to extract the verified .NET SDK archive."
fi
[[ -f "${payload_dir}/sdk/${required_version}/dotnet.dll" ]] || fail "Verified archive does not contain .NET SDK ${required_version}."

mkdir -p "${install_dir}"
if cp --help 2>&1 | grep -Fq -- '--update[='; then
    cp -R --update=none "${payload_dir}/." "${install_dir}/"
else
    cp -R -n "${payload_dir}/." "${install_dir}/"
fi
[[ -f "${install_dir}/sdk/${required_version}/dotnet.dll" ]] || fail "Failed to install .NET SDK ${required_version}."

echo "Installed .NET SDK ${required_version} side-by-side in ${install_dir}."
