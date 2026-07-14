#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat >&2 << 'EOF'
Usage: bash scripts/generate-openapi-artifacts.sh <admin|catalog> [--refresh]
EOF

    return 1
}

service="${1:-}"
refresh="${2:-}"

if [[ $# -lt 1 || $# -gt 2 || (-n "${refresh}" && "${refresh}" != "--refresh") ]]; then
    usage
fi

case "${service}" in
    admin)
        project="src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj"
        property="GenerateAdminOpenApiArtifacts"
        if [[ "${refresh}" == "--refresh" ]]; then
            property="RefreshAdminOpenApiArtifacts"
        fi
        ;;
    catalog)
        project="src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj"
        property="GenerateCatalogOpenApiArtifacts"
        if [[ "${refresh}" == "--refresh" ]]; then
            property="RefreshCatalogOpenApiArtifacts"
        fi
        ;;
    *)
        usage
        ;;
esac

dotnet_build_arguments=("${project}" "-p:${property}=true")
if [[ "${CI:-}" == "true" ]]; then
    dotnet_build_arguments=(--no-restore "${dotnet_build_arguments[@]}")
fi

env \
    OpenApi__BuildGeneration=true \
    Authentication__Authority=https://openapi.invalid \
    Authentication__Issuer=https://openapi.invalid \
    dotnet build "${dotnet_build_arguments[@]}"
