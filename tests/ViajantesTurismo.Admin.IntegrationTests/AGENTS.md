# AGENTS.md

Instructions for files under `tests/ViajantesTurismo.Admin.IntegrationTests/`.

This file narrows the broader `tests/AGENTS.md` guidance for API integration test work.

## Scope and precedence

- Applies to all files under `tests/ViajantesTurismo.Admin.IntegrationTests/`.
- If instructions conflict with `tests/AGENTS.md`, follow this file for integration-specific work.

## What this project tests

- Full HTTP → command → domain → persistence → query → JSON round-trips.
- HTTP status codes for all CRUD and lifecycle operations.
- Round-trip correctness of calculated values (e.g., `TotalPrice` after discount).
- Idempotency of safe-to-repeat operations.
- Empty-list behavior under a controlled database state (serial only).

Business rule permutations and exhaustive validation belong in unit or behavior tests, not here.

## Base class rules

- Parallel tests: inherit `AdminApiIntegrationTestBase`.
- Serial (empty-state) tests: inherit `AdminApiSerialTestBase`; decorate with
  `[Trait("SeedDependency", "Intentional-EmptyState-Smoke")]`.
- Do not inherit any other base class or implement `IAsyncLifetime` directly.

## Data setup

- Always create test data through the API — never insert directly into the database.
- Use `Client.CreateTestTour(...)`, `Client.CreateTestCustomer(...)`,
  `Client.CreateTestBooking(...)` from `TestFixtureHelpers`.
- Use `DtoBuilders` to construct all request DTOs.
- Use `TestDataGenerator` for unique emails, national IDs, and tour identifiers.

## Parallel safety

- Never assert absolute counts on full list responses.
- Filter list results by a `HashSet<Guid>` of your own IDs before asserting counts.

## Helpers

Use the existing helpers before adding new plumbing:

- `BookingsApiHelper` — all booking endpoint operations
- `CustomersApiHelper` — `CreateCustomerAsync`, `GetAllCustomersAsync`
- `ToursApiHelper` — `CreateTourAsync`, `GetAllToursAsync`
- `PricingHelper.CalculateExpectedBookingPrice(...)` — expected `TotalPrice` computation
- `TestDefaults` — named constants for default tour prices

## Naming conventions

Use descriptive, natural-language test names with underscores throughout. Do not append
fixed suffixes like `Expected_Behavior`.

| Test type                     | Form                                           |
|-------------------------------|------------------------------------------------|
| Happy path                    | `Can_Verb_Object`                              |
| Not found                     | `Returns_not_found_for_an_invalid_id`          |
| Validation failure            | `Returns_bad_request_for_an_invalid_reason`    |
| Conflict / illegal transition | `Returns_conflict_for_an_illegal_transition`   |
| Idempotency                   | `Ignores_a_duplicate_request`                  |
| Serial empty-state smoke      | `Returns_an_empty_list_when_no_records_exist`  |

Do not append `Async` to test method names.

## Co-locating parallel and serial classes

When a domain area has both a parallel test class and an empty-list serial class, place both
in the same file (parallel class first, serial class after).

## References

- `tests/AGENTS.md`
- `tests/ViajantesTurismo.Admin.IntegrationTests/README.md`
- `docs/TEST_GUIDELINES.md`
