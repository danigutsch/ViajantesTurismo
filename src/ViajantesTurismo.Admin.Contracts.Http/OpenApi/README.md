# Admin OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Admin.

Current boundary files:

- `tours.openapi.json`
- `customers.openapi.json`
- `bookings.openapi.json`
- `documents.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Admin.ApiService`.
- The refresh build generates intermediate documents under `OpenApi/.generated/`.
- The .NET tool starts the trusted document generator with a minimal environment. It preserves
  authorization metadata without registering JWT/OIDC; normal application runs stay fail-closed.
- CI drift checks can generate only intermediate documents with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate admin
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate admin --refresh
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
