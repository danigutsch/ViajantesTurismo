# ViajantesTurismo.Admin.UiIntegrationTests

Hosted UI integration tests for Admin-facing surfaces.

This project is intentionally scaffolded before the first real tests are moved.
Keep it scaffold-only until a concrete Admin route-composition scenario clearly belongs here instead
of in `ViajantesTurismo.Management.WebTests` or browser-driven `ViajantesTurismo.Admin.SystemTests`.

Target scope:

- hosted route and page composition below browser system depth
- app wiring and UI integration behavior that is broader than component tests
- cheaper than browser-driven `ViajantesTurismo.Admin.SystemTests`
