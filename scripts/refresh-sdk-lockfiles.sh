#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "${script_dir}/.." && pwd)"

cd "${repo_root}"

dotnet restore ViajantesTurismo.slnx --force-evaluate
dotnet restore ViajantesTurismo.slnx --locked-mode
