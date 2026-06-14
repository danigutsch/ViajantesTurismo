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

1. **Assembly Fixtures**: Use a shared Aspire-hosted `ApiFixture` for the parallel-safe Admin integration lane and
   a shared Aspire-hosted `AspireSystemTestFixture` for the parallel-safe browser lane. Each attribute lives in its
   own test assembly:

    Integration test assembly:

    ```csharp
    [assembly: Xunit.AssemblyFixture(typeof(ApiFixture))]
    ```

    System test assembly:

    ```csharp
    [assembly: AssemblyFixture(typeof(AspireSystemTestFixture))]
    ```

2. **Hybrid Parallel/Serial Shape**:
   - **Parallel Tests**: ordinary integration and browser system classes run concurrently by default.
   - **Serial Tests**: only the narrow clean-slate lanes opt in to sequential execution via
     `AspireSerialSystemTestBase` or the corresponding serial integration collection.

3. **Fixture-Owned Baseline Control**: Seeded defaults can be shared for parallel-safe reads, while destructive
   clean-slate reset stays inside serial fixture or base-class infrastructure.

4. **Parallel-Safe Patterns**:
   - Read-only tests use shared seeded data without mutation.
   - Data-mutator tests create unique data via HTTP API helpers rather than modifying seeded records.
   - Serial tests use infrastructure-owned reset paths for exact-count assertions and destructive database operations.

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
- **Single Source of Truth**: Aspire-managed fixtures define the canonical hosted test model.

### Cons

- **Discipline Required**: Developers must ensure parallel tests don't assert exact data counts or mutate seeded
records. Code review and the ADR documentation enforce this.
- **Serial Tests Slower**: The 6 serial tests run sequentially after all parallel tests finish, but they are in the
  minority.
- **Debugging Concurrency Issues**: Race conditions in parallel tests are harder to diagnose than in sequential tests
  (though well-designed tests should not have them).

## Alternatives Considered

1. **Two Collection Fixtures** — Each collection would get its own hosted fixture instance, creating extra Aspire apps
   and databases. Rejected: defeats the purpose of parallelization and increases infrastructure overhead.

2. **IClassFixture per Test** — Each test class gets its own fixture. Rejected: same overhead as above, and no parallel
   benefit.

3. **No Parallelization** — Keep sequential execution. Rejected: wastes CI/CD time and development velocity.

## Links

- [xUnit v3 Assembly Fixtures Documentation](https://xunit.net/docs/shared-context#assembly-fixtures)
- [Architecture Decisions Index](../ARCHITECTURE_DECISIONS.md)
