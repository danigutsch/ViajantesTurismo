#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
tool_dir="${repo_root}/.tmp/dotnet-tools"
stryker="${tool_dir}/dotnet-stryker"

if [[ ! -x "${stryker}" ]]; then
    mkdir -p "${tool_dir}"
    dotnet tool install dotnet-stryker --version 4.15.0 --tool-path "${tool_dir}"
fi

configs=(
    "tests/ViajantesTurismo.Catalog.UnitTests/stryker-domain-config.json"
    "tests/ViajantesTurismo.Catalog.UnitTests/stryker-application-config.json"
    "tests/ViajantesTurismo.Admin.UnitTests/stryker-application-config.json"
)

for config in "${configs[@]}"; do
    project_dir="${config%/*}"
    config_file="${config##*/}"

    echo "==> ${config}"
    (
        cd "${repo_root}/${project_dir}"
        "${stryker}" --config-file "${config_file}"
    )
done
