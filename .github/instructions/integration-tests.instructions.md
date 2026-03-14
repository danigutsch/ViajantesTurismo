---
description: "Use when creating, editing, or debugging integration tests in ViajantesTurismo.Admin.IntegrationTests. Enforces the project's base class, parallelism, naming, helper usage, HTTP status mapping, and database-isolation conventions."
name: "ViajantesTurismo Integration Test Guidelines"
applyTo: "tests/ViajantesTurismo.Admin.IntegrationTests/**/*.cs"
---

# Integration Test Guidelines

Extends the general test guidelines in `.github/instructions/tests.instructions.md`.
Rules here are specific to `tests/ViajantesTurismo.Admin.IntegrationTests` and take precedence
within that project when they conflict with the general file.

## Infrastructure and base classes

- All parallel integration test classes inherit `AdminApiIntegrationTestBase`.
- All serial integration test classes inherit `AdminApiSerialTestBase`.
  Serial tests are declared in `[Collection("Integration.Serial")]` (defined via the
  `IntegrationSerialTests` collection-definition class).
- Do **not** inherit any other base class or implement `IAsyncLifetime` directly in test classes.
- Access the HTTP client via the inherited `Client` property (`HttpClient`).
- Use `TestContext.Current.CancellationToken` for every async operation; do not pass
  `CancellationToken.None`.
- Use `Guid.CreateVersion7()` to generate non-existent IDs in not-found scenarios.

## When to use serial vs parallel

- Use `AdminApiIntegrationTestBase` (parallel) for all tests that create their own data and
  assert on that data only.
- Use `AdminApiSerialTestBase` (serial) only when the test must assert on the complete state of
  the database — for example, an empty-list smoke that calls `Assert.Empty(...)` against the
  full collection. Decorate those tests with
  `[Trait("SeedDependency", "Intentional-EmptyState-Smoke")]`.
- In serial tests, call `await ClearDatabaseAsync(TestContext.Current.CancellationToken)` in
  the Arrange block to make the emptied-database intent explicit, even though `InitializeAsync`
  already performs a clear-then-seed cycle.

## Parallel safety

- Never assert absolute counts on full list responses in parallel tests.
- Filter responses by a known set of IDs (`HashSet<Guid>`) before asserting counts or ordering.
- Ensure every entity created in a test has a unique identifier — use `TestDataGenerator` and
  `TestFixtureHelpers` to generate collision-free data.

## Test setup — use TestFixtureHelpers

- Create persisted tours with `Client.CreateTestTour(identifier?, name?)`.
  `CreateTestTour` follows the HTTP `Location` header and returns a fully-hydrated `GetTourDto`.
- Create persisted customers with `Client.CreateTestCustomer(firstName, lastName)`.
  `CreateTestCustomer` reads the response body directly and returns a `GetCustomerDto`.
- Create persisted bookings with `Client.CreateTestBooking(tourId, customerId, companionId?)`.
  Defaults to `RoomTypeDto.DoubleOccupancy` and `BikeTypeDto.Regular`.
- Do **not** insert entities directly into the database; always go through the API.

## Helper usage

- Use `BookingsApiHelper` extension methods on `Client` for all booking endpoint calls (create,
  get, list, confirm, cancel, complete, update-notes, update-discount, update-details, delete,
  record-payment).
- Use `CustomersApiHelper` for `CreateCustomerAsync` and `GetAllCustomersAsync`.
- Use `ToursApiHelper` for `CreateTourAsync` and `GetAllToursAsync`.
- For endpoints not covered by the helpers (e.g., `GetById`, `Update`) call `Client` directly
  with typed methods (`GetAsync`, `PutAsJsonAsync`, `PatchAsJsonAsync`).
- Use `DtoBuilders` to construct all request DTOs — do not build request objects inline with
  object initializers in test methods.

## Pricing assertions

- When asserting `TotalPrice` on a booking, compute the expected value using
  `PricingHelper.CalculateExpectedBookingPrice(...)` rather than hardcoding a decimal literal.
- Reference `TestDefaults.BaseTourPrice` and `TestDefaults.RegularBikePrice` to stay aligned
  with `DtoBuilders` defaults; do not duplicate the numeric values.

## HTTP status code conventions

Integration tests verify the full HTTP contract. Assert the status code first, then deserialize.

| Operation result | Expected status |
|---|---|
| Successful creation | `201 Created` |
| Successful read or state-transition returning a body | `200 OK` |
| Successful full-replace update (no body) | `204 NoContent` |
| Non-existent resource | `404 NotFound` |
| Duplicate / illegal state transition | `409 Conflict` |
| Domain validation failure (`ValidationProblem`) | `400 BadRequest` |

## Naming conventions

Follow `Method_Name_Context_Description_Expected_Behavior` using underscores throughout.

Preferred forms by test type:

- **Happy path:** `Can_Verb_Object` or `Can_Verb_Object_With_Detail`
  — `Can_Create_Tour`, `Can_Create_Booking_With_Companion`
- **Not found:** `Verb_Object_Returns_Not_Found_For_Invalid_Id`
  — `Get_Tour_By_Id_Returns_Not_Found_For_Invalid_Id`
- **Bad request / validation:** `Verb_Object_Returns_Bad_Request_For_Reason`
  — `Create_Tour_Returns_Bad_Request_For_Invalid_Price`
- **Conflict / illegal transition:** `Verb_Object_Returns_Conflict`
  — `Confirm_Cancelled_Booking_Returns_Conflict`
- **Idempotency:** `Verb_Object_Is_Idempotent`
  — `Confirm_Already_Confirmed_Booking_Is_Idempotent`
- **Multi-step workflow:** describe the full flow, e.g.
  `Payment_Workflow_Multiple_Partial_Payments_To_Full_Payment`
- **Empty-state serial smoke:** `Can_Get_Empty_Object_List`

Do not mix PascalCase run-on and underscore forms in the same test class.
Do not add an `Async` suffix to test method names.

## Assertions

- Assert the HTTP status code before deserializing the response body.
- Use `Assert.Equal(expected, actual)` for status code checks.
- In multi-step tests, insert inline guard assertions after prerequisite steps so the test fails
  at the right step. These guard assertions belong in the Arrange block structure, not in a
  separate Act/Assert cycle.
- Assert only business-visible outcomes (status codes, field values, counts filtered by
  known IDs). Do not assert framework or infrastructure internals.

## What integration tests cover

Integration tests validate the full HTTP → command → domain → persistence → query → JSON
round-trip. They do not replace unit or behavior tests.

Appropriate for integration tests:

- HTTP status codes for all CRUD and lifecycle operations.
- Correct response body shape and field values.
- Round-trip correctness of calculated values (e.g., `TotalPrice` after discount).
- API contract for error responses (status code; optionally message substring).
- Idempotency of safe-to-repeat operations.
- Empty-list behavior under a controlled database state (serial only).
- Basic multi-step workflows (e.g., partial payment progression).

Not appropriate for integration tests (keep in unit or behavior tests):

- Exhaustive permutations of domain validation rules.
- All state-machine transition combinations beyond the happy path and key illegal-transition examples.
- Pagination, filtering, and sorting of list endpoints.
- Authentication and authorization.
- Cache behavior.

## Co-locating parallel and serial classes

When a domain area requires both a parallel class and an empty-list serial class, place both
in the same file. The parallel class is declared first; the serial class follows in the same file.
Example: `GetAllToursTests.cs` contains `GetAllToursTests` (parallel) and
`GetAllToursEmptyListTests` (serial).

## References

- `docs/TEST_GUIDELINES.md`
- `.github/instructions/tests.instructions.md`
- `tests/ViajantesTurismo.Admin.IntegrationTests/README.md`
- `tests/ViajantesTurismo.Admin.Tests.Shared/Integration/Helpers/`
