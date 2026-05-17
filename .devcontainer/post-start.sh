#!/bin/bash

set -euo pipefail

dotnet_env_file="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/.dotnet-env"
if [[ -f "${dotnet_env_file}" ]]; then
    # Keep post-start lifecycle commands on the same SDK selected during on-create.
    # shellcheck source=.devcontainer/.dotnet-env
    source "${dotnet_env_file}"
fi

dotnet dev-certs https --trust || true
