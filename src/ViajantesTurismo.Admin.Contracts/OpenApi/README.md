# Admin OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Admin.

Current boundary files:

- `tours.openapi.json`
- `customers.openapi.json`
- `bookings.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Admin.ApiService`.
- `dotnet build` generates intermediate documents under `OpenApi/.generated/`.
- Refresh the committed canonical artifacts intentionally with
  `dotnet build src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj -p:RefreshAdminOpenApiArtifacts=true`.
- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
