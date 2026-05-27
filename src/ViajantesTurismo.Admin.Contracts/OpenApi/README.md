# Admin OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Admin.

Current boundary files:

- `tours.openapi.json`
- `customers.openapi.json`
- `bookings.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Admin.ApiService`.
- These files are generated during `dotnet build` of the solution or the Admin API project.
- `OpenApi/.generated/` is intermediate output only.
- The renamed `*.openapi.json` files in this folder are the canonical contract artifacts for consumers and contract tests.
