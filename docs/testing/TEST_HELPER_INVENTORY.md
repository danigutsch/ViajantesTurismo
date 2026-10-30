# Test helper inventory

Owner: repository maintainers. Scope: helper classes that SKTEST004 cleanup moved out of xUnit test
classes, plus the shared test-support projects under `tests/`. Update this page when a helper gains
or loses callers, or when a consolidation is accepted or rejected. Entries are curated; caller counts
are the number of C# files under `tests/` that reference the type, checked with `grep -rlw`.

See [Test guidelines](../TEST_GUIDELINES.md) for how tests use these helpers.

## Contents

- [Classification rules](#classification-rules)
- [Inventory](#inventory)
- [Consolidated in this change](#consolidated-in-this-change)
- [Rejected consolidations](#rejected-consolidations)

## Classification rules

- **Local-only:** one project or one test area uses it, or it encodes area-specific values. It stays
  next to its callers.
- **Project-level reusable:** several test projects in this repository use it. It lives in a
  `*.Testing` support project or a shared `Infrastructure` folder.
- **Shared-library candidate:** it has no application knowledge and more than one repository would
  use it. It belongs in SharedKernel, and the application consumes it through the pinned package.
- A helper moves only when it has two or more real callers or clear framework-level value.

## Inventory

| Helper | Location | Callers | Classification |
| --- | --- | --- | --- |
| `AspireTestApplication` | SharedKernel package | 8 | Shared library (already consumed, not copied) |
| `PostgreSqlPublicSchemaReset` | SharedKernel package | 3 | Shared library (already consumed, not copied) |
| `PostgreSqlTestDatabasePool` | `PandaBear.Npgsql.Testing` package | wrapped by `PostgreSqlTestServerFixture` | Shared library (already consumed) |
| `FakeTimeProvider` | `tests/ViajantesTurismo.Admin.Testing/Fakes` | 21 | Project-level reusable. Adds `AutoAdvance` and `ReadCount` beyond a plain fixed clock |
| `FakeUnitOfWork`, `FakeCustomerStore`, `FakeTourStore` | `Admin.Testing/Fakes` | 36, 23, 13 | Project-level reusable. They implement Admin application ports |
| `ThrowingUnitOfWork`, `CapturingDomainEventDispatcher`, `NoOpTourCapacityMutationLock` | `Admin.Testing/Fakes` | 4, 3, 6 | Project-level reusable. Admin ports, few callers |
| `EntityBuilders`, `DtoBuilders`, `BookingTestHelpers` | `Admin.Testing` | 46, 15, 20 | Project-level reusable. Domain-specific sample data |
| `ApiTestExtensions`, `ToursApiHelper`, `BookingsApiHelper`, `CustomersApiHelper`, `PricingHelper` | `Admin.Testing/Integration/Helpers` | shared by Admin API, hosted and UI suites | Project-level reusable. Admin API surface only |
| `DocumentNoStoreHeaders` | `Admin.Testing` | 5 | Project-level reusable |
| `HostedTestFixture` | `HostedTests/Infrastructure` | 22 | Project-level reusable inside `HostedTests` |
| `PostgreSqlTestServerFixture` and `PostgreSqlTestServerFixtureTestQueries` | `HostedTests/Infrastructure` | 28 | Project-level reusable. Depends on `HostedTestFixture`, so it stays in this repository |
| `HostedTestCleanup`, `AdvisoryLockWaitScope` | `HostedTests/Infrastructure` | 8, 3 | Local-only to `HostedTests` |
| `Catalog.Testing` stores, locks and `CatalogApiTestHost` | `tests/ViajantesTurismo.Catalog.Testing/Infrastructure` | shared by Catalog unit, API and behavior suites | Project-level reusable. Catalog ports only |
| `CollectingLogger` | `Catalog.Testing/Infrastructure` | 11 | Project-level reusable. A shared recording-logger replacement is planned |
| `DisposalTrackingStream` | `Catalog.Testing/Infrastructure` | 2 | Local-only until stream doubles are reviewed |
| `ReadObservingStream`, `GatedChunkContent`, `SlowUpload`, `RawHttpUpload` and related wire probes | `tests/ViajantesTurismo.UploadWire.Testing` | shared by upload wire tests | Project-level reusable. Upload wire behavior only |

## Consolidated in this change

- **`PostgreSqlTestServerFixture`.** The Admin and Management copies were identical
  apart from namespace and doc wording. One definition now lives in
  `tests/ViajantesTurismo.HostedTests/Infrastructure`, together with
  `PostgreSqlTestServerFixtureTestQueries`. Both fixture test classes remain because their traits and
  assertions differ.
- **Catalog time providers.** `ControlledTimeProvider` and `FixedTimeProvider` were
  single-purpose copies of a clock fake. They are removed in favor of the repository's custom
  `FakeTimeProvider`, so there is one fake clock rather than three.
- **Output cache store.** The local `RecordingOutputCacheStore` in the Catalog API service tests is removed in
  favor of the SharedKernel `RecordingOutputCacheStore`.
- **Blocking read streams.** The Admin document and branding contract test copies of a blocking read stream are
  replaced by the SharedKernel `BlockingReadStream`. Stream doubles with chunking, cancellation tracking or
  throwing behavior stay local.
- **Logger captures and remaining generic doubles.** Not consolidated yet. The pinned SharedKernel build does not
  ship a recording logger, and its shared browser file, form file and distributed cache fakes do not cover the
  local variants' recorded state or selective failures.
- **Local HTTP handler stubs.** Not consolidated yet. The pinned SharedKernel build does not ship a shared
  test HTTP message handler, so the Management web, branding contract and hosted-test handlers stay local
  until a published build includes it.

## Rejected consolidations

- **Admin API helpers.** `ToursApiHelper`, `BookingsApiHelper`, `CustomersApiHelper` and
  `PricingHelper` encode Admin routes and DTO shapes. Moving them into a shared library would leak
  application knowledge. They also have no second consumer outside this repository.
- **UI and Playwright helpers.** Page abstractions and browser helpers follow the markup of one
  application. Only the generic browser plumbing is a library candidate, and SharedKernel already
  provides it.
- **Area-specific values.** Sample customers, tours, prices and dates stay with the area that owns
  them. Merging them into one data class would create a junk drawer and hide which value matters to
  which test.
- **Broad reset, fixture and container abstractions.** A single base fixture or reset helper across
  Admin, Catalog and Management would couple suites that start different services and have different
  isolation needs. Share only narrow, proven pieces such as the database pool and schema reset.
- **Moving `PostgreSqlTestServerFixture` to SharedKernel.** It depends on the application-specific
  `HostedTestFixture`. Extracting it needs a connection-string abstraction that two callers in one
  project do not justify.
- **Replacing Admin `FakeTimeProvider` with a generic clock fake.** Callers rely on `ReadCount` and
  `AutoAdvance`, which a plain clock fake does not provide.
