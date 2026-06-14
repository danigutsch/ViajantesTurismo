# Issue 137 Implementation Plan: Admin Aspire-Hosted Test Migration

Issue links:

- Primary issue [`#137`](https://github.com/danigutsch/ViajantesTurismo/issues/137)
- Viability spike [`#106`](https://github.com/danigutsch/ViajantesTurismo/issues/106)
- E2E reliability driver [`#100`](https://github.com/danigutsch/ViajantesTurismo/issues/100)
- Fixture seam umbrella [`#59`](https://github.com/danigutsch/ViajantesTurismo/issues/59)
- Drift guards [`#116`](https://github.com/danigutsch/ViajantesTurismo/issues/116)

Decisions captured for this plan:

- [x] Prioritize `SystemTests` payoff ahead of broad `IntegrationTests` conversion because the strongest immediate value is reliability work tracked in `#100`.
- [x] Keep `ViajantesTurismo.Admin.UiIntegrationTests` empty until a clear extraction candidate exists.
- [x] Change the hosted test seam direction so test bodies do not directly perform database reset, seeding, or generic host lifecycle plumbing.
- [x] Default all hosted tests to parallel-safe design.
- [x] If parallel execution is truly impossible for a scenario, the opt-in serial behavior
      must be applied by base class or fixture infrastructure rather than by ad hoc
      test-body plumbing.

## Target end state

- `IntegrationTests` use Aspire-hosted execution as the canonical full-host path.
- `SystemTests` use Aspire-hosted execution as the canonical browser path without hybrid
  host layering.
- Test bodies depend on business-visible seams only:
    - `IntegrationTests`: typed API clients and domain-visible assertions
    - `SystemTests`: browser-visible routes and semantic UI assertions
- Reset/seed/baseline plumbing is fixture-owned infrastructure, not a test-body seam.
- Parallel safety is the default architecture.
- Serial execution remains available only through an explicit base-class or collection-fixture path for the narrow scenarios that cannot be made parallel-safe.
- Legacy host paths remain only as temporary migration scaffolding and are removed at the end.

## Explicit seam changes from current state

Current state that this plan intends to retire:

- a scaffold-only `UiIntegrationTests` lane that must stay empty until a real hosted UI
  composition slice appears
- temporary migration notes that still describe retired hybrid host plumbing
- any architecture drift that would let deleted `E2E*` fixtures or direct reset seams return

Target seam direction:

- Remove reset and seed from the test-body-facing hosted seam.
- Keep lifecycle operations inside fixture infrastructure and base classes.
- Allow only narrow, named, behavior-oriented setup helpers where needed for deterministic owned-data creation.
- Do not expose `IServiceProvider`, generic scope access, or test-body database lifecycle methods.
- Keep browser tests dependent on browser-visible entrypoints and project-owned setup helpers, not host control methods.

## Sequencing

### Phase 0: Re-baseline docs and guards for the new seam

Branch name: `docs/issue-137-admin-hosted-test-seam-rebaseline`

- [ ] Update `tests/README.md` and `tests/TEST_FIXTURE_SEAM.md` so the canonical hosted seam no longer advertises test-body `Seed` / `Reset` usage.
- [ ] State clearly that reset and baseline control belong in fixture/base-class infrastructure.
- [ ] State clearly that serial execution is an infrastructure-level exception path, not a test-body responsibility.
- [ ] Update `AdminTestArchitectureGuardTests` so they protect the new seam instead of freezing the current one.
- [ ] Keep `UiIntegrationTests` documented as intentionally empty until a real extraction appears.

Suggested commit split:

- [ ] Commit 1: rebaseline hosted seam docs
- [ ] Commit 2: update architecture guards for the new seam direction

### Phase 1: Inventory and classify the Admin hosted test surface

Branch name: `docs/issue-137-admin-hosted-test-inventory`

- [ ] Inventory `IntegrationTests` by slice, fixture dependency, and reset dependency.
- [ ] Inventory `SystemTests` by owned-data behavior, browser workflow depth, and serial/reset dependency.
- [ ] Classify each current hosted test area into:
    - parallel-safe and ready for Aspire migration
    - Aspire-migratable after fixture-owned baseline work
    - temporarily legacy during transition
- [ ] Record where current tests still call host lifecycle methods directly from test code.
- [ ] Identify the first concrete `SystemTests` slice for migration under `#100`.
- [ ] Identify the first concrete `IntegrationTests` slice to migrate after the initial system-test payoff slice lands.

Suggested commit split:

- [ ] Commit 1: document hosted test inventory and migration buckets
- [ ] Commit 2: record first system and integration target slices

### Phase 2: Migrate the first `SystemTests` slice to pure Aspire-hosted execution

Branch name: `test/issue-137-systemtests-aspire-first-slice`

- [x] Replace the current `E2EFixture` hybrid bootstrapping path with an
      Aspire-hosted path that discovers and uses the real AppHost-managed web and API
      resources.
- [ ] Keep the browser seam limited to web entrypoint plus project-owned setup helpers.
- [ ] Move any reset/baseline behavior behind fixture/base-class infrastructure.
- [ ] Keep default test execution parallel-safe.
- [ ] Preserve a dedicated serial base path only for scenarios that truly require exclusive baseline control.
- [ ] Migrate one owned-data, non-serial system-test slice first.
- [ ] Validate repeated runs to measure reliability impact for `#100`.

Suggested first migration targets:

- shared navigation or feedback tests that already use owned data
- a tours or customers slice that navigates directly by known IDs

Suggested commit split:

- [ ] Commit 1: introduce Aspire-hosted system fixture without exposing lifecycle controls to tests
- [ ] Commit 2: migrate first parallel-safe system-test slice
- [ ] Commit 3: tighten validation and reliability notes for `#100`

### Phase 3: Migrate serial `SystemTests` scenarios behind automatic infrastructure control

Branch name: `test/issue-137-systemtests-serial-baseline-control`

- [ ] Refactor the serial system-test path so baseline control remains automatic inside the serial base class or fixture layer.
- [ ] Remove direct reset/seed expectations from serial test bodies.
- [ ] Keep serial scope as narrow as possible and document why those cases cannot yet be parallel-safe.
- [ ] Migrate one representative serial class that currently depends on hard reset behavior.
- [ ] Confirm the remaining parallel-safe classes are unaffected.

Suggested commit split:

- [ ] Commit 1: move serial baseline control fully behind infrastructure
- [ ] Commit 2: migrate first reset-heavy serial class
- [ ] Commit 3: document the narrow serial exception model

### Phase 4: Migrate the first `IntegrationTests` slice to Aspire-hosted execution

Branch name: `test/issue-137-integrationtests-aspire-first-slice`

- [ ] Introduce or finalize an Aspire-hosted integration fixture built on `Aspire.Hosting.Testing` and AppHost resource ownership.
- [ ] Keep test bodies on typed API-client or request/response seams only.
- [ ] Remove direct host lifecycle calls from test bodies in the migrated slice.
- [ ] Migrate the first representative integration slice already proven viable in `#106`.
- [ ] Keep legacy `ApiFixture` only for non-migrated tests during the transition.

Suggested first migration targets:

- tours list/read slice
- bookings workflow slice

Suggested commit split:

- [ ] Commit 1: land Aspire-hosted integration fixture with narrow test-body seam
- [ ] Commit 2: migrate first integration slice
- [ ] Commit 3: validate coverage workflow still works for migrated Aspire-hosted tests

### Phase 5: Migrate baseline-sensitive `IntegrationTests` behind fixture-owned control

Branch name: `test/issue-137-integrationtests-baseline-control`

- [ ] Convert empty-state or clean-slate integration scenarios so baseline control is fixture-owned instead of test-body-owned.
- [ ] Preserve parallel-safe defaults for the rest of the integration suite.
- [ ] Keep any serial or exclusive baseline path isolated behind explicit infrastructure rather than broad assembly-wide serialization.
- [ ] Migrate one representative empty-state or baseline-sensitive integration slice.

Suggested commit split:

- [ ] Commit 1: infrastructure-owned baseline control for integration tests
- [ ] Commit 2: migrate first baseline-sensitive slice

### Phase 6: Retire legacy hosted fixtures and drift

Branch name: `test/issue-137-admin-hosted-legacy-cleanup`

- [ ] Remove legacy hosted fixture paths once both canonical Aspire-hosted lanes are established.
- [ ] Remove now-obsolete hybrid-host assumptions from docs and test helpers.
- [ ] Tighten architecture guards so hybrid and direct lifecycle seams do not drift back in.
- [ ] Reconfirm `UiIntegrationTests` remains empty unless a real extraction candidate has appeared.

Suggested commit split:

- [ ] Commit 1: remove retired legacy fixture code
- [ ] Commit 2: tighten drift guards and docs after cleanup

## Child issue breakdown

Recommended follow-up issues to create from this plan:

1. `Tests: rebaseline Admin hosted test seam docs and architecture guards for Aspire-first migration`
2. `SystemTests: migrate first parallel-safe Admin browser slice to pure Aspire-hosted fixture`
3. `SystemTests: move serial baseline control behind infrastructure-owned reset flow`
4. `IntegrationTests: migrate first Admin API slice to Aspire-hosted fixture`
5. `IntegrationTests: move clean-slate baseline control behind fixture-owned infrastructure`
6. `Tests: remove legacy hybrid Admin hosted fixtures after Aspire migration`

## Validation expectations

- Always validate the narrowest changed class or namespace first.
- For `SystemTests`, repeat migrated-slice runs to watch for reliability regressions or improvements relevant to `#100`.
- For `IntegrationTests`, rerun the migrated namespace and the existing coverage workflow used by the Aspire-hosted spike.
- Before full cleanup, verify coexistence of migrated and non-migrated lanes on the same branch.
- Do not accept a migration that lowers flakiness only by broadly disabling parallelism.

## Non-goals

- Do not reopen the viability question already answered by `#106`.
- Do not force immediate population of `ViajantesTurismo.Admin.UiIntegrationTests`.
- Do not preserve direct test-body lifecycle APIs just for migration convenience.
- Do not make assembly-wide serialization the default answer to fixture isolation problems.

## Notes

- The strongest near-term business value is reducing system-test instability while aligning the repo to the canonical Aspire-hosted direction.
- The migration should leave the repo with fewer host models, fewer public lifecycle seams, and clearer parallel-safety defaults than it has today.
