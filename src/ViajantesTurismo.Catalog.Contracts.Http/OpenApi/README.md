# Catalog OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Catalog.

Current boundary files:

- `catalog.openapi.json`
- `public-catalog.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Catalog.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- The .NET tool scopes the build-generation marker to the document generator process. Deterministic,
  no-discovery placeholder authentication applies only there; normal application runs stay fail-closed.
- CI drift checks can generate only intermediate documents with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate catalog
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate catalog --refresh
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
