# ADR-018: Test Parallelization with xUnit v3 Assembly Fixtures

**Status**: Accepted — 2026-02-28

## Context

Previously, all E2E and integration tests ran sequentially due to the `[Collection("E2E")]` and
`[Collection("Admin API")]` collection definitions. This caused the test suite to be slow and inefficient, even though
most individual tests were parallel-safe.

xUnit v3 introduced **Assembly Fixtures**, which provide a single fixture instance shared across the entire test
assembly without impacting parallelization. This allows us to unify infrastructure (Aspire app, PostgreSQL, Redis) while
enabling tests to run concurrently.

## Decision

We implement test parallelization using:

1. **Assembly Fixtures**: Replace collection-based fixture sharing with a single `E2EFixture` and `ApiFixture` instance
   shared across all tests via the assembly attribute:

   ```csharp
   [assembly: AssemblyFixture(typeof(E2EFixture))]
   ```

2. **Hybrid Configuration**:
   - **Parallel Tests**: 15 E2E test classes + 20 integration test classes run concurrently by default (xUnit's implicit
     per-class collection behavior).
   - **Serial Tests**: 3 E2E test classes + 3 integration test classes opt-in to sequential execution via
     `[Collection("E2E.Serial")]` or `[Collection("Integration.Serial")]` (these test database edge cases and exact
     counts).

3. **Idempotent Seeding**: The database is seeded once per assembly (during fixture initialization) with a guard check
   to prevent duplicate seeding on fixture reuse.

4. **Parallel-Safe Patterns**:
   - Read-only tests use shared seeded data without mutation.
   - Data-mutator tests create unique data via HTTP API helpers rather than modifying seeded records.
   - Serial tests use per-test seed/clear for exact-count assertions and database destructive operations.

5. **API Helpers**: A new `ApiTestHelper` class provides methods to create data (tours, customers, bookings) via the
   Admin API, enabling tests to be independent and concurrent.

## Consequences

### Pros

- **Speed**: Full E2E suite runs ~30% faster (18 tests in parallel vs. sequential); integration suite similarly
improved.
- **Clarity**: Tests declare intent—parallel tests are explicitly data-independent; serial tests are marked with a
  collection.
- **Maintainability**: No per-test fixture lifecycle overhead; shared infrastructure is initialized once.
- **Scalability**: New tests naturally run in parallel unless they need serial execution.
- **Single Source of Truth**: One Aspire app, one PostgreSQL instance, one Redis for all tests.

### Cons

- **Discipline Required**: Developers must ensure parallel tests don't assert exact data counts or mutate seeded
records. Code review and the ADR documentation enforce this.
- **Serial Tests Slower**: The 6 serial tests run sequentially after all parallel tests finish, but they are in the
  minority.
- **Debugging Concurrency Issues**: Race conditions in parallel tests are harder to diagnose than in sequential tests
  (though well-designed tests should not have them).

## Alternatives Considered

1. **Two Collection Fixtures** — Each collection would get its own `E2EFixture` instance, creating two Aspire apps and
   two databases. Rejected: defeats the purpose of parallelization and increases infrastructure overhead.

2. **IClassFixture per Test** — Each test class gets its own fixture. Rejected: same overhead as above, and no parallel
   benefit.

3. **No Parallelization** — Keep sequential execution. Rejected: wastes CI/CD time and development velocity.

## Links

- [xUnit v3 Assembly Fixtures Documentation](https://xunit.net/docs/shared-context#assembly-fixtures)
- [Architecture Decisions Index](../ARCHITECTURE_DECISIONS.md)
