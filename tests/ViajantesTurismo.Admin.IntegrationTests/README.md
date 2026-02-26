# ViajantesTurismo.Admin.IntegrationTests

API integration tests — endpoint wiring, serialization, and happy/edge-path validation using real infrastructure
via .NET Aspire.

## Scope & Strategy

- One wiring test per endpoint (success + not found) in `BookingApiTests`, `CustomerApiTests`, `ToursApiTests`.
- Advanced multi-endpoint scenarios (discount, payment, room/companion changes) in `BookingApiAdvancedTests`.
- Business rule permutations belong in unit/behavior tests — not here.
- Validation errors asserted only when testing API contract translation (Result → ValidationProblem / NotFound).

## Notes

- Infrastructure (Postgres) launched once per session via `ApiFixture` using `DistributedApplicationBuilder`.
- No mocking — helpers create real entities via public endpoints.
- Expected prices computed with a local helper mirroring the domain formula.

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
