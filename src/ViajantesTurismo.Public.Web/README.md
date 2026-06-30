# ViajantesTurismo.Public.Web

Public-facing website for customer discovery and interactions.

This project follows ADR-020 by keeping public audience concerns separate from internal management workflows. It should
not reference `ViajantesTurismo.Management.Web` or operator-only UI dependencies.

## Current scope

- Public landing route at `/` with Catalog-backed hero content.
- Published tour listing route at `/group-bike-tours`.
- Published tour detail route at `/group-bike-tours/{Slug}` with gallery rendering.
- `en-US` and `pt-BR` editable content variants requested through the Catalog public content API.
- Configurable public website theme settings rendered as SSR-friendly CSS variables with safe defaults.
- Standard service defaults and health endpoints through `MapDefaultEndpoints()`.
- Aspire AppHost registration as `ResourceNames.PublicWebApp`.
