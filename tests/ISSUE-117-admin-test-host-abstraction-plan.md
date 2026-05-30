# Issue 117 Implementation Plan: Admin Test Host Abstraction

Issue links:

- Original issue [`#117`](https://github.com/danigutsch/ViajantesTurismo/issues/117)
- Follow-up issue [`#140`](https://github.com/danigutsch/ViajantesTurismo/issues/140)
- Umbrella issue [`#59`](https://github.com/danigutsch/ViajantesTurismo/issues/59)

- [x] Define `IAdminTestHost` interface with minimal operations shared by integration and E2E tests:
    - `HttpClient Client { get; }`
    - `Uri BaseUri { get; }`
    - `Task Seed(CancellationToken cancellationToken = default)`
    - `Task Reset(CancellationToken cancellationToken = default)`

## PR Breakdown

### PR 1: Land the narrow integration seam

Branch name: `feature/issue-140-admin-test-host-integration-seam`

- [ ] Implement `IAdminTestHost` for existing `ApiFixture` (integration tests)
- [ ] Update one integration test (for example Bookings or Tours happy-path) to use the abstraction
- [ ] Validate the narrow integration slice

Suggested commit split:

- [ ] Commit 1: implement `IAdminTestHost` on the integration fixture
- [ ] Commit 2: migrate one integration test to the new seam
- [ ] Commit 3: tighten validation coverage for the integration seam

### PR 2: Extend the seam to Aspire-backed or E2E usage

Branch name: `feature/issue-140-admin-test-host-aspire-rollout`

- [ ] Implement `IAdminTestHost` for Aspire-hosted fixture (if present)
- [ ] Update one Aspire-hosted or E2E test to use the abstraction
- [ ] Validate test pass using both current and new host seam

Suggested commit split:

- [ ] Commit 1: implement the abstraction on the Aspire-backed fixture
- [ ] Commit 2: migrate one Aspire or E2E test to the new seam
- [ ] Commit 3: add validation for side-by-side seam support

### PR 3: Canonicalize and document the seam

Branch name: `docs/issue-140-admin-test-host-canonicalization`

- [ ] Document the abstraction in `tests/README.md` and cross-link in ADRs if needed
- [ ] Announce the seam as canonical in test or developer docs
- [ ] Record incremental migration guidance for remaining tests

Suggested commit split:

- [ ] Commit 1: document the abstraction and allowed usage
- [ ] Commit 2: mark the seam as canonical in repo docs
- [ ] Commit 3: add incremental migration notes for remaining fixtures and tests

Subsequent work: migrate other test fixtures and cases incrementally under `#140` and `#59`.
