#!/usr/bin/env bash

set -euo pipefail

version=""
package_dir=""
output_dir="artifacts/release-prep"
source_tag=""
release_impact=""
sha=""

usage() {
    cat << 'USAGE'
Usage: bash scripts/prepare-release-artifacts.sh --version <semver> --package-dir <path>
       [--output-dir <path>] [--source-tag <tag>] [--release-impact <impact>] [--sha <sha>]

Creates release-prep notes and a package manifest for dry-run review.
USAGE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)
            version="$2"
            shift 2
            ;;
        --package-dir)
            package_dir="$2"
            shift 2
            ;;
        --output-dir)
            output_dir="$2"
            shift 2
            ;;
        --source-tag)
            source_tag="$2"
            shift 2
            ;;
        --release-impact)
            release_impact="$2"
            shift 2
            ;;
        --sha)
            sha="$2"
            shift 2
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
    echo "Missing required --version" >&2
    exit 1
fi

if [[ -z "${package_dir}" ]]; then
    echo "Missing required --package-dir" >&2
    exit 1
fi

if [[ ! -d "${package_dir}" ]]; then
    echo "Package directory does not exist: ${package_dir}" >&2
    exit 1
fi

if [[ -z "${sha}" ]]; then
    sha="$(git rev-parse HEAD)"
fi

commit_range="HEAD"
if [[ -n "${source_tag}" ]]; then
    commit_range="${source_tag}..HEAD"
fi

mkdir -p "${output_dir}"

release_notes="${output_dir}/release-notes.md"
changelog="${output_dir}/CHANGELOG.md"
manifest="${output_dir}/release-manifest.json"

{
    echo "# Release ${version}"
    echo
    echo "- Commit: \`${sha}\`"
    if [[ -n "${source_tag}" ]]; then
        echo "- Previous release tag: \`${source_tag}\`"
    else
        echo "- Previous release tag: none"
    fi
    if [[ -n "${release_impact}" ]]; then
        echo "- Release impact: \`${release_impact}\`"
    fi
    echo
    echo "## Changes"
    echo
    git log --format='- %s (%h)' "${commit_range}" || true
} > "${release_notes}"

{
    echo "# Changelog"
    echo
    cat "${release_notes}"
} > "${changelog}"

VERSION="${version}" \
    PACKAGE_DIR="${package_dir}" \
    OUTPUT_MANIFEST="${manifest}" \
    SOURCE_TAG="${source_tag}" \
    RELEASE_IMPACT="${release_impact}" \
    SOURCE_SHA="${sha}" \
    python3 - << 'PY'
import hashlib
import json
import os
from pathlib import Path

package_dir = Path(os.environ["PACKAGE_DIR"])
packages = []

for path in sorted(package_dir.glob("*.nupkg")):
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    packages.append(
        {
            "fileName": path.name,
            "sha256": digest,
            "sizeBytes": path.stat().st_size,
        }
    )

manifest = {
    "version": os.environ["VERSION"],
    "sourceSha": os.environ["SOURCE_SHA"],
    "sourceTag": os.environ["SOURCE_TAG"] or None,
    "releaseImpact": os.environ["RELEASE_IMPACT"] or None,
    "packages": packages,
    "sbom": None,
    "sbomNote": "No repository SBOM generator is currently wired into release prep.",
}

Path(os.environ["OUTPUT_MANIFEST"]).write_text(
    json.dumps(manifest, indent=2) + "\n",
    encoding="utf-8",
)
PY

echo "Release prep artifacts: ${output_dir}"
