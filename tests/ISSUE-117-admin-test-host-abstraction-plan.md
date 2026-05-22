# Issue 117 Implementation Plan: Admin Test Host Abstraction

- [x] Define `IAdminTestHost` interface with minimal operations shared by integration and E2E tests:
    - `HttpClient Client { get; }`
    - `Uri BaseUri { get; }`
    - `Task Seed(CancellationToken cancellationToken = default)`
    - `Task Reset(CancellationToken cancellationToken = default)`
- [ ] Implement `IAdminTestHost` for existing `ApiFixture` (integration tests)
- [ ] Update one integration test (e.g., Bookings or Tours happy-path) to use abstraction
- [ ] Implement `IAdminTestHost` for Aspire-hosted fixture (if present)
- [ ] Update one Aspire-hosted test to use abstraction
- [ ] Document new abstraction in `tests/README.md` and cross-link in ADRs if needed
- [ ] Validate test pass using both current and new host seam
- [ ] Announce the seam as canonical, reference in test/developer docs

Subsequent work: migrate other test fixtures and cases incrementally to abstraction.
