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
- The following Bash or Git Bash commands scope placeholder authentication URLs and the build-generation
  marker to the document-generator child process. Do not export them for normal application runs or
  ordinary builds.
- CI drift checks can generate only intermediate documents with:

  ```bash
  env OpenApi__BuildGeneration=true Authentication__Authority=https://openapi.invalid Authentication__Issuer=https://openapi.invalid dotnet build src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj -p:GenerateAdminOpenApiArtifacts=true
  ```

- Refresh the committed canonical artifacts intentionally with:

  ```bash
  env OpenApi__BuildGeneration=true Authentication__Authority=https://openapi.invalid Authentication__Issuer=https://openapi.invalid dotnet build src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj -p:RefreshAdminOpenApiArtifacts=true
  ```

- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
