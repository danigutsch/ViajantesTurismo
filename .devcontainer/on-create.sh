#!/bin/bash

set -euo pipefail

echo "🔧 Running one-time container setup..."

WORKSPACE_FOLDER="${1:-${PWD}}"
BUILD_DIR="${WORKSPACE_FOLDER}/.build"
DOTNET_INSTALL_DIR="/home/vscode/.dotnet"
DOTNET_ROOT_MARKER_BEGIN="# >>> ViajantesTurismo DOTNET_ROOT >>>"
DOTNET_ROOT_MARKER_END="# <<< ViajantesTurismo DOTNET_ROOT <<<"

required_sdk_version="$(python3 - "${WORKSPACE_FOLDER}/global.json" <<'PY'
import json
import sys

with open(sys.argv[1], 'r', encoding='utf-8') as file:
    print(json.load(file)['sdk']['version'])
PY
)"

# Ensure expected writable caches/directories exist for the non-root user.
sudo mkdir -p \
    /home/vscode/.nuget \
    /home/vscode/.nuget/packages \
    /home/vscode/.local/share/NuGet/v3-cache \
    "${DOTNET_INSTALL_DIR}" \
    "${BUILD_DIR}"

sudo chown -R vscode:vscode \
    /home/vscode/.nuget \
    /home/vscode/.local/share/NuGet \
    "${DOTNET_INSTALL_DIR}" \
    "${BUILD_DIR}"

use_local_dotnet=false

if [[ -d "${DOTNET_INSTALL_DIR}/sdk/${required_sdk_version}" ]]; then
    use_local_dotnet=true
elif ! dotnet --list-sdks | grep -q "^${required_sdk_version} "; then
    install_script="$(mktemp)"
    trap 'rm -f "${install_script}"' EXIT

    curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "${install_script}"
    bash "${install_script}" --version "${required_sdk_version}" --install-dir "${DOTNET_INSTALL_DIR}"
    use_local_dotnet=true
fi

if [[ "${use_local_dotnet}" == "true" ]]; then
    export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
    export PATH="${DOTNET_INSTALL_DIR}:${PATH}"
fi

resolved_sdk_version="$(dotnet --version)"

for profile in /home/vscode/.bashrc /home/vscode/.profile; do
    python3 - "${profile}" "${DOTNET_ROOT_MARKER_BEGIN}" "${DOTNET_ROOT_MARKER_END}" "${use_local_dotnet}" <<'PY'
from pathlib import Path
import sys

profile_path = Path(sys.argv[1])
marker_begin = sys.argv[2]
marker_end = sys.argv[3]
use_local_dotnet = sys.argv[4] == "true"

content = profile_path.read_text(encoding='utf-8') if profile_path.exists() else ""
block = (
    f"{marker_begin}\n"
    'export DOTNET_ROOT="/home/vscode/.dotnet"\n'
    'export PATH="$DOTNET_ROOT:$PATH"\n'
    f"{marker_end}\n"
)

start = content.find(marker_begin)
if start != -1:
    end = content.find(marker_end, start)
    if end != -1:
        end += len(marker_end)
        while end < len(content) and content[end] == "\n":
            end += 1
        content = content[:start] + content[end:]

if use_local_dotnet:
    if content and not content.endswith("\n"):
        content += "\n"
    content += block

profile_path.write_text(content, encoding='utf-8')
PY
done

if [[ "${resolved_sdk_version}" != "${required_sdk_version}" ]]; then
    echo "Resolved .NET SDK ${resolved_sdk_version} does not match required ${required_sdk_version}." >&2
    exit 1
fi

dotnet tool restore

# Configure repository as safe for git once.
if ! git config --global --get-all safe.directory | grep -Fxq "${WORKSPACE_FOLDER}"; then
    git config --global --add safe.directory "${WORKSPACE_FOLDER}"
fi

echo "✅ One-time container setup completed."
