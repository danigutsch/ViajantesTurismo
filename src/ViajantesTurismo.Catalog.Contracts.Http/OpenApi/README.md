# Catalog OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Catalog.

Current boundary files:

- `catalog.openapi.json`
- `public-catalog.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Catalog.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- The following Bash or Git Bash commands scope placeholder authentication URLs and the build-generation
  marker to the document-generator child process. Do not export them for normal application runs or
  ordinary builds.
- CI drift checks can generate only intermediate documents with:

  ```bash
  env OpenApi__BuildGeneration=true Authentication__Authority=https://openapi.invalid Authentication__Issuer=https://openapi.invalid dotnet build src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj -p:GenerateCatalogOpenApiArtifacts=true
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  env OpenApi__BuildGeneration=true Authentication__Authority=https://openapi.invalid Authentication__Issuer=https://openapi.invalid dotnet build src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj -p:RefreshCatalogOpenApiArtifacts=true
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
