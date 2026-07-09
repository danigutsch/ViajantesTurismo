#!/usr/bin/env bash

set -euo pipefail

dotnet run --project tools/SharedKernel.Documentation.Tool -- generate --config docs/architecture/generated-diagrams.json "$@"
