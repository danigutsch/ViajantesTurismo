# Branding OpenAPI artifacts

This directory contains the committed, canonical OpenAPI documents for Branding HTTP boundaries.

Ownership rules:

- `*.openapi.json` files are canonical contract artifacts and ship with
  `ViajantesTurismo.Branding.Contracts.Http`.
- CI drift checks generate intermediate documents with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate branding
  ```

- Refresh canonical artifacts intentionally with:

  ```bash
  dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate branding --refresh
  ```

- `OpenApi/.generated/` is intermediate output only and must not be committed.
