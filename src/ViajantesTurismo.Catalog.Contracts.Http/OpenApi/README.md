# Catalog OpenAPI Artifacts

This folder contains the canonical generated OpenAPI boundary artifacts for Catalog.

Current boundary files:

- `catalog.openapi.json`
- `public-catalog.openapi.json`
- `v1.openapi.json`

Ownership rules:

- Source metadata lives in `src/ViajantesTurismo.Catalog.Api`.
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

## Catalog navigation links

Catalog JSON resources include a `links` array of objects with `rel`, `href`, and `method`. Tour and
content collections retain their JSON array shape with links on each item. Existing fields and the
typed client return types stay compatible. Clients can read responses from servers that omit links.

Public tour items link by slug. A public tour detail also links to the tour collection. A localized
content response links to its canonical key and the culture it serves. Management tour responses
link by Catalog ID and expose their image collection. Management content responses link to the
editable content read. Image responses link to the related galleries and stored rendition previews.
These links use the registered public or authenticated Catalog routes. The binary rendition
endpoints continue to return image bodies.
