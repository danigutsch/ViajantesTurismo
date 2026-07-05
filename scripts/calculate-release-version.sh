#!/usr/bin/env bash

set -euo pipefail

if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
    echo "GITHUB_OUTPUT is required" >&2
    exit 1
fi

source_sha="${GITHUB_SHA:-}"
if [[ -z "${source_sha}" ]]; then
    source_sha="$(git rev-parse HEAD)"
fi

source_tag="$(git describe --tags --match 'v[0-9]*' --abbrev=0 2> /dev/null || true)"
base_version="0.1.0"
commit_range="HEAD"

if [[ -n "${source_tag}" ]]; then
    base_version="${source_tag#v}"
    commit_range="${source_tag}..HEAD"
fi

tool_args=(
    compute
    --base "${base_version}"
    --sha "${source_sha}"
)

release_version_kind="${RELEASE_VERSION_KIND:-prerelease}"

if [[ "${release_version_kind}" == "stable" ]]; then
    :
elif [[ -n "${GITHUB_RUN_NUMBER:-}" ]]; then
    tool_args+=(--prerelease "alpha.${GITHUB_RUN_NUMBER}")
fi

tool_project="tools/SharedKernel.Versioning.Tool/SharedKernel.Versioning.Tool.csproj"
dotnet build "${tool_project}" --no-restore --verbosity quiet
version_json="$(git log --format='%B%x00' "${commit_range}" | dotnet run --project "${tool_project}" --no-build --no-restore -- "${tool_args[@]}")"

VERSION_JSON="${version_json}" \
    BASE_VERSION="${base_version}" \
    SOURCE_TAG="${source_tag}" \
    python3 - << 'PY'
import json
import os

version = json.loads(os.environ["VERSION_JSON"])
outputs = {
    "base_version": os.environ["BASE_VERSION"],
    "source_tag": os.environ["SOURCE_TAG"],
    "version_json": json.dumps(version, separators=(",", ":")),
    "sem_ver": version["semVer"],
    "release_impact": version["releaseImpact"],
    "package_version": version["packageVersion"],
    "assembly_version": version["assemblyVersion"],
    "file_version": version["fileVersion"],
    "informational_version": version["informationalVersion"],
}

with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
    for name, value in outputs.items():
        output.write(f"{name}={value}\n")
PY

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    {
        echo "## Version calculation"
        echo
        echo "- Base version: \`${base_version}\`"
        if [[ -n "${source_tag}" ]]; then
            echo "- Source tag: \`${source_tag}\`"
        else
            echo "- Source tag: none"
        fi
        echo "- Output: \`${version_json}\`"
    } >> "${GITHUB_STEP_SUMMARY}"
fi
