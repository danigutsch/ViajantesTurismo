#!/usr/bin/env bash

set -euo pipefail

configs=(
    "tests/ViajantesTurismo.Common.UnitTests/stryker-config.json"
    "tests/ViajantesTurismo.Catalog.UnitTests/stryker-domain-config.json"
    "tests/ViajantesTurismo.Catalog.UnitTests/stryker-application-config.json"
    "tests/ViajantesTurismo.Admin.UnitTests/stryker-application-config.json"
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
    "tests/SharedKernel.Results.GeneratorTests/stryker-config.json"
    "tests/SharedKernel.Mediator.Analyzers.Tests/stryker-config.json"
    "tests/SharedKernel.Style.Analyzers.Tests/stryker-config.json"
    "tests/SharedKernel.Testing.Analyzers.Tests/stryker-config.json"
)

for config in "${configs[@]}"; do
    project_dir="${config%/*}"
    config_file="${config##*/}"

    echo "==> ${config}"
    (
        cd "${project_dir}"
        dotnet tool run dotnet-stryker -- --config-file "${config_file}"
    )
done
