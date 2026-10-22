#!/usr/bin/env bash
set -euo pipefail

# Keep local and CI callers on the same .NET implementation.
dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -c Release --no-restore -- run-ci-test-slice "$@"
