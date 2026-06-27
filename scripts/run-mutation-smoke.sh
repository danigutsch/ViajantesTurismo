#!/usr/bin/env bash

set -euo pipefail

configs=(
    "tests/SharedKernel.BuildingBlocks.Tests/stryker-config.json"
    "tests/SharedKernel.Domain.Tests/stryker-config.json"
    "tests/SharedKernel.DomainEvents.Tests/stryker-config.json"
    "tests/SharedKernel.EventSourcing.Tests/stryker-config.json"
    "tests/SharedKernel.Functional.Tests/stryker-config.json"
    "tests/SharedKernel.Idempotency.Tests/stryker-config.json"
    "tests/SharedKernel.IntegrationEvents.Tests/stryker-config.json"
    "tests/SharedKernel.IntegrationEvents.CloudEvents.Tests/stryker-config.json"
    "tests/SharedKernel.Observability.Tests/stryker-config.json"
    "tests/SharedKernel.Results.AspNet.Tests/stryker-config.json"
)

for config in "${configs[@]}"; do
    project_dir="${config%/*}"

    echo "==> ${project_dir}"
    (
        cd "${project_dir}"
        dotnet tool run dotnet-stryker
    )
done
