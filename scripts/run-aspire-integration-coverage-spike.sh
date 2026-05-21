#!/usr/bin/env bash

set -euo pipefail

project="tests/ViajantesTurismo.Admin.IntegrationTests/ViajantesTurismo.Admin.IntegrationTests.csproj"
dll="tests/ViajantesTurismo.Admin.IntegrationTests/bin/Debug/net10.0/ViajantesTurismo.Admin.IntegrationTests.dll"
coverage_settings="coverage.settings.xml"
results_dir="TestResults/aspire-integration-coverage-spike"
coverage_output="${results_dir}/coverage.cobertura.xml"
test_namespace="ViajantesTurismo.Admin.IntegrationTests.AspireHosted"

mkdir -p "${results_dir}"

session_id="$({ python3 - <<'PY'
import uuid
print(uuid.uuid4())
PY
} 2>/dev/null)"

if [[ -z "${session_id}" ]]; then
    echo "Failed to generate dotnet-coverage session id." >&2
    exit 1
fi

cleanup() {
    dotnet tool run dotnet-coverage shutdown "${session_id}" >/dev/null 2>&1 || true
}

trap cleanup EXIT

dotnet build "${project}"

dotnet tool run dotnet-coverage instrument \
    --session-id "${session_id}" \
    --settings "${coverage_settings}" \
    "${dll}"

dotnet tool run dotnet-coverage collect \
    --session-id "${session_id}" \
    --settings "${coverage_settings}" \
    --server-mode \
    --background \
    --output "${coverage_output}" \
    --output-format cobertura

dotnet tool run dotnet-coverage connect "${session_id}" \
    dotnet test --project "${project}" --no-build --filter-namespace "${test_namespace}"

echo "Coverage spike output written to ${coverage_output}"
