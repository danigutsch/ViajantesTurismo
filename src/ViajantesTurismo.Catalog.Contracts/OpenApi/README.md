# Catalog OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Catalog.

Current boundary files:

- `catalog.openapi.json`
- `public-catalog.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Catalog.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- Refresh the committed canonical artifacts intentionally with
  `dotnet build src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj -p:RefreshCatalogOpenApiArtifacts=true`.
- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
