# Issue 115 Admin Contract Tests Plan

Issue link: [`#115`](https://github.com/danigutsch/ViajantesTurismo/issues/115)

Prerequisite issue: [`#141`](https://github.com/danigutsch/ViajantesTurismo/issues/141)

- [ ] Confirm canonical source-of-truth direction:
    - `src/ViajantesTurismo.Admin.Contracts` is the public Admin boundary package
    - boundary-specific OpenAPI artifacts live under `src/ViajantesTurismo.Admin.Contracts/OpenApi/`
    - `ViajantesTurismo.Admin.ContractTests` validates provider and consumer sides against the same artifact

- [ ] Remove the temporary PactNet-first approach for the tours OpenAPI slice

- [ ] Keep contract tests boundary-focused and avoid broad API behavior duplication

## PR Breakdown

### PR 0: Shared OpenAPI foundation prerequisite

Issue: `#141`

Branch name: `feature/issue-141-sharedkernel-openapi-foundation`

- [x] Land `SharedKernel.OpenApi` as the reusable foundation for boundary OpenAPI registration
- [x] Keep this prerequisite PR OpenAPI-focused and avoid a generic API framework abstraction

### PR 1: Generate and expose canonical OpenAPI artifacts

Branch name: `feature/issue-115-admin-openapi-artifacts`

- [ ] Add build-time OpenAPI generation for Admin API boundaries
- [ ] Produce boundary-specific artifacts:
    - `Tours.openapi.json`
    - `Customers.openapi.json`
    - `Bookings.openapi.json`
- [ ] Publish or copy generated artifacts into `src/ViajantesTurismo.Admin.Contracts/OpenApi/`
- [ ] Document artifact ownership and regeneration flow in the right docs

Suggested commit split:

- [ ] Commit 1: enable build-time OpenAPI generation for Admin API
- [ ] Commit 2: expose generated boundary artifacts from `Admin.Contracts`
- [ ] Commit 3: document canonical contract artifact flow

### PR 2: Replace current contract-test implementation with unified artifact validation

Branch name: `test/issue-115-admin-contract-artifact-validation`

- [ ] Remove the current PactNet tours OpenAPI consumer slice
- [ ] Add provider validation:
    - API-generated OpenAPI document matches the canonical contract artifact
- [ ] Add consumer/client validation:
    - client-facing contract usage remains compatible with the canonical contract artifact
- [ ] Keep the first real slice narrow to `Tours`
- [ ] Update `tests/ViajantesTurismo.Admin.ContractTests/README.md` to describe the unified model

Suggested commit split:

- [ ] Commit 1: remove PactNet-specific first slice plumbing
- [ ] Commit 2: add provider-side canonical artifact validation
- [ ] Commit 3: add consumer/client-side canonical artifact validation
- [ ] Commit 4: refresh contract-test project docs

### PR 3: Expand and harden the boundary model

Branch name: `test/issue-115-admin-contract-boundary-expansion`

- [x] Extend the same pattern to `Customers`
- [x] Extend the same pattern to `Bookings`
- [x] Add guardrails so artifact drift is caught clearly in CI
- [ ] Review whether generated artifacts should be committed, packed, or both

Suggested commit split:

- [x] Commit 1: add customers contract validation
- [x] Commit 2: add bookings contract validation
- [x] Commit 3: add CI or validation guardrails for artifact drift

## Notes

- For provider-owned OpenAPI boundaries, canonical generated artifacts are
  cleaner than keeping a mock-server-only Pact consumer as the primary
  contract source of truth.
- PactNet can still be used later where a real interaction-driven consumer-provider workflow is needed.
