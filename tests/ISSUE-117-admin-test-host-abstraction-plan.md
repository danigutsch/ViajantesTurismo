# Issue 117 Implementation Plan: Admin Test Host Abstraction

- [ ] Define `IAdminTestHost` interface with minimal, intention-revealing operations for both integration and E2E tests:
    - e.g., GetApiClient(), GetBaseUrl(), Seed(), Reset(), Clear()
- [ ] Implement `IAdminTestHost` for existing `ApiFixture` (integration tests)
- [ ] Update one integration test (e.g., Bookings or Tours happy-path) to use abstraction
- [ ] Implement `IAdminTestHost` for Aspire-hosted fixture (if present)
- [ ] Update one Aspire-hosted test to use abstraction
- [ ] Document new abstraction in `tests/README.md` and cross-link in ADRs if needed
- [ ] Validate test pass using both current and new host seam
- [ ] Announce the seam as canonical, reference in test/developer docs

Subsequent work: migrate other test fixtures and cases incrementally to abstraction.
