# Admin OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Admin.

Current boundary files:

- `tours.openapi.json`
- `customers.openapi.json`
- `bookings.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Admin.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- The wrapper scopes placeholder authentication URLs and the build-generation marker to the document
  generator child process. Do not export them for normal application runs or ordinary builds.
- CI drift checks can generate only intermediate documents with:

  ```bash
  bash scripts/generate-openapi-artifacts.sh admin
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  bash scripts/generate-openapi-artifacts.sh admin --refresh
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
