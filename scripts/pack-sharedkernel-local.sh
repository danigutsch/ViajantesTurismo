#!/usr/bin/env bash

set -euo pipefail

version=""
output_root="artifacts/packages/local"
verify_restore=true

usage() {
    cat << 'USAGE'
Usage: bash scripts/pack-sharedkernel-local.sh [--version <semver>] [--output-root <path>]
       [--skip-restore-check]

Packs src/SharedKernel/*/*.csproj into artifacts/packages/local/<version>/ and verifies the
packages restore from that local feed. Reusing an existing package version fails fast.
USAGE
}

require_command() {
    if ! command -v "$1" > /dev/null 2>&1; then
        echo "Required command not found: $1" >&2
        exit 1
    fi
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)
            if [[ $# -lt 2 ]]; then
                echo "Missing value for --version" >&2
                exit 1
            fi
            version="$2"
            shift 2
            ;;
        --output-root)
            if [[ $# -lt 2 ]]; then
                echo "Missing value for --output-root" >&2
                exit 1
            fi
            output_root="$2"
            shift 2
            ;;
        --skip-restore-check)
            verify_restore=false
            shift
            ;;
        -h | --help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

if [[ -z "${version}" ]]; then
    version="0.1.0-alpha.local.$(date -u +%Y%m%d%H%M%S)"
fi

package_dir="${output_root}/${version}"

require_command dotnet
if [[ "${verify_restore}" == true ]]; then
    require_command python3
fi

shopt -s nullglob
existing_packages=(
    "${package_dir}"/SharedKernel.*."${version}".nupkg
    "${package_dir}"/SharedKernel.*."${version}".snupkg
    "${package_dir}"/SharedKernel.*."${version}".symbols.nupkg
)
if [[ ${#existing_packages[@]} -gt 0 ]]; then
    echo "Package version already exists in ${package_dir}: ${version}" >&2
    exit 1
fi

mkdir -p "${package_dir}"

projects=(src/SharedKernel/*/*.csproj)
if [[ ${#projects[@]} -eq 0 ]]; then
    echo "No SharedKernel projects found." >&2
    exit 1
fi

for project in "${projects[@]}"; do
    dotnet pack "${project}" \
        -c Release \
        -p:ComputedSemVer="${version}" \
        -o "${package_dir}"
done

if [[ "${verify_restore}" == true ]]; then
    python3 scripts/verify-sharedkernel-local-feed.py "${package_dir}" "${version}"
fi

echo "SharedKernel packages: ${package_dir}"
