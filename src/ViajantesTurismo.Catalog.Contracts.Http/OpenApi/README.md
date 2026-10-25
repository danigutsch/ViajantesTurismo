# Catalog OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Catalog.

Current boundary files:

- `catalog.openapi.json`
- `public-catalog.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Catalog.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- After a locked restore, `sharedkernel-ci openapi` starts the trusted document generator with a minimal environment. It preserves
  authorization metadata without registering JWT/OIDC; normal application runs stay fail-closed.
- CI drift checks can generate only intermediate documents with:

  ```bash
  dotnet tool run sharedkernel-ci -- openapi generate --target catalog
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  dotnet tool run sharedkernel-ci -- openapi refresh --target catalog
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
