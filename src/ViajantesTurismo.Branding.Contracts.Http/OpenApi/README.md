# Branding OpenAPI artifacts

This directory contains the committed, canonical OpenAPI documents for Branding HTTP boundaries.

Ownership rules:

- `*.openapi.json` files are canonical contract artifacts and ship with
  `ViajantesTurismo.Branding.Contracts.Http`.
- After a locked restore, `sharedkernel-ci openapi` starts the trusted document generator with a minimal environment. It preserves
  authorization metadata without registering JWT/OIDC; normal application runs stay fail-closed.
- CI drift checks generate intermediate documents with:

  ```bash
  dotnet tool run sharedkernel-ci -- openapi generate --target branding
  ```

- Refresh canonical artifacts intentionally with:

  ```bash
  dotnet tool run sharedkernel-ci -- openapi refresh --target branding
  ```

- `OpenApi/.generated/` is intermediate output only and must not be committed.
