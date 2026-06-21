# ViajantesTurismo.Public.Web

Public-facing website for customer discovery and interactions.

This project follows ADR-020 by keeping public audience concerns separate from internal management workflows. It should
not reference `ViajantesTurismo.Management.Web` or operator-only UI dependencies.

## Current scope

- Minimal public landing route at `/`.
- Standard service defaults and health endpoints through `MapDefaultEndpoints()`.
- Aspire AppHost registration as `ResourceNames.PublicWebApp`.
