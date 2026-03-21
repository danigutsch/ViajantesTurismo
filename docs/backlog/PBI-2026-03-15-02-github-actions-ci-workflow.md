# PBI-2026-03-15-02 — Add GitHub Actions CI workflow for build, test, and quality gates (IMP-027)

## Metadata

- **ID:** PBI-2026-03-15-02
- **Improvement:** IMP-027
- **Title:** Add GitHub Actions CI workflow for build, test, and quality gates
- **Priority:** High
- **Status:** Completed
- **Type:** Infrastructure / Developer Experience / Quality
`main`-branch changes are not validated by a shared, repeatable automation contract before merge.

This PBI adds a baseline CI workflow that runs on GitHub-hosted runners, provisions the repository's pinned .NET and
Node toolchains, restores dependencies, builds the solution, runs automated tests, executes repository quality checks,
and publishes useful diagnostics when something fails. The baseline path should stay lean and conventional: use
standard runner jobs for normal PR validation, and treat devcontainer validation as a supplemental environment-parity
check rather than the default gate for every change.

Branch protection is configured on `main`, and a representative pull request has completed successfully with the
required checks `Build and Test`, `Lint`, and `Dependency Review`; the dependency review gate is implemented by the
separate `.github/workflows/dependency-review.yml` workflow described in `docs/CI_GOVERNANCE_ROLLOUT.md`. Any
remaining CI enhancements should be treated as follow-up work, not blockers for the baseline rollout. Coverage is
published both as GitHub Actions artifacts and through a dedicated SonarCloud analysis workflow;
planned follow-up work should focus on policy tuning, badges, SHA pinning, and keeping SonarCloud
enforced as a required merge gate.

## Problem Statement

Today, contributors rely on local discipline to run the correct combination of commands before opening or merging a
pull request. In practice, that creates avoidable risk:

- local environments may drift from repository expectations
- contributors may forget to run one of the required checks
- analyzers, tests, or linting may pass locally for one person and fail elsewhere
- reviewers must spend time inferring whether the branch is actually merge-safe
- failures are discovered late instead of at pull-request time

In a repository that combines .NET projects, Node-based linting, xUnit v3 with Microsoft.Testing.Platform,
coverage/report generation, and stricter analyzer settings, the absence of CI leaves a gap between
"it works on my machine" and "it is safe to merge."

## Why This Matters

A repository-owned CI workflow creates a consistent quality gate that is:

- **Shared** — everyone is validated the same way
- **Repeatable** — build/test/lint behavior does not depend on personal setup quirks
- **Visible** — contributors and reviewers can see pass/fail state directly on the pull request
- **Actionable** — failures produce logs and artifacts that reduce guesswork
- **Extensible** — new checks can be added without relying on tribal knowledge

## Current Repository Facts

The workflow should reflect the repository's current, documented command surface instead of inventing a parallel one.
As of this PBI:

- The repository pins **.NET SDK `10.0.100`** in `global.json`.
- The repository uses **Microsoft.Testing.Platform** as the configured test runner in `global.json`.
- `package.json` declares **Node `>=24.0.0 <25.0.0`** and **npm `>=10.0.0`**.
- The repository includes `package-lock.json`, so deterministic Node installation with `npm ci` is appropriate.
- The documented build command is `dotnet build ViajantesTurismo.slnx`.
- The documented full test command is `dotnet test --solution ViajantesTurismo.slnx`.
- The documented repository-wide quality command is `npm run lint:all`.
- Coverage guidance already exists via `coverage.settings.xml` and `reportgenerator` aggregation patterns.
- A `.devcontainer/` configuration exists, but no GitHub Actions workflows currently exist under `.github/workflows/`.

Those facts strongly support a standard GitHub-hosted runner workflow using official setup actions rather than a
container-first design for the default PR gate.

## Research Summary

Online guidance reviewed for this PBI points to a pragmatic baseline:

- GitHub recommends using `actions/setup-dotnet` and `actions/setup-node` for pinned tool provisioning on hosted
  runners.
- Built-in caching from the setup actions is usually preferable to custom cache-key logic unless finer control is
  required.
- GitHub recommends publishing artifacts for test results, logs, and other diagnostics, especially with `if: always()`
  so failure details are not lost.
- Workflow concurrency can cancel superseded branch or PR runs, reducing wasted time and speeding feedback.
- Container jobs are supported, but they introduce runtime differences such as shell behavior and should be used
  intentionally.
- Dev Containers are a valid CI automation target, but they are best used for environment parity, smoke validation, or
  prebuilt-image workflows rather than as the default validation path for every routine pull request.
- GitHub recommends granting the `GITHUB_TOKEN` the **minimum required permissions** and tightening permissions at the
  workflow or job level rather than accepting broad defaults.
- GitHub security guidance recommends pinning third-party workflow dependencies to immutable versions, ideally full
  commit SHAs, or at minimum using a clearly documented update strategy for trusted actions.
- GitHub's protected-branch guidance warns that required status checks depend on **stable, unique job names**; duplicate
  names across workflows can create ambiguous merge requirements.
- GitHub recommends using `CODEOWNERS` to protect workflow files and other automation-critical assets from unreviewed
  changes.
- GitHub's artifact guidance supports explicit artifact names and retention settings, and validates artifact integrity
  automatically with digests during upload/download.
- GitHub's caching guidance explicitly warns against storing credentials or other sensitive material in cache paths.

## Reference Repository Observations

Additional patterns reviewed in `dotnet/aspire` and `foxminchan/BookWorm` reinforce the same direction while adding
some practical refinements.

### Observations from `dotnet/aspire`

- Aspire separates concerns across multiple workflows rather than forcing every validation type into a single file.
- Aspire maintains CI-specific documentation describing workflow architecture, test routing, failure-analysis tooling,
  and optimization strategy, which makes the pipeline easier to evolve safely.
- Aspire documents that workflow/job names and helper scripts must stay aligned when automation depends on those names,
  which reinforces the need for stable naming in required checks.
- Aspire stores CI diagnostics under predictable paths such as `testresults/`, making artifact upload and failure
  analysis easier.
- Aspire distinguishes baseline CI, outer-loop tests, quarantined tests, deployment tests, and transient-failure
  recovery workflows, which is a useful long-term model once a baseline workflow is established.
- Aspire explicitly documents approved GitHub Actions in at least part of its automation surface, reinforcing the value
  of a pinning or approved-actions policy.

### Observations from `foxminchan/BookWorm`

- BookWorm exposes workflow health and quality signals prominently in its `README.md`, including CI and coverage
  badges, making the automation contract visible to contributors.
- BookWorm documents its CI/CD architecture as an ADR, including workflow roles, security and quality configuration,
  dependency management, and concurrency strategy.
- BookWorm uses workflow specialization (`dotnet` CI, PR validation, docs, SonarQube, cleanup, frontend, deployment)
  rather than a single overloaded workflow.
- BookWorm's documented strategy includes concurrency, path-based triggers, reusable workflow/action components, and
  explicit dependency-update policy via Dependabot.
- BookWorm elevates security scanning, secret detection, and quality analysis as first-class CI concerns rather than
  postscript nice-to-haves.
- BookWorm also pairs CI expectations with contributor guidance and repository governance, which helps turn workflow
  YAML into an actual team contract.

## Decision Direction

For this repository, the baseline pull-request CI should **not** run the entire build-and-test pipeline inside the
repository devcontainer by default.

Instead:

- the **primary workflow** should run directly on a GitHub-hosted runner
- toolchains should be provisioned with `actions/setup-dotnet` and `actions/setup-node`
- the workflow should run the same core commands contributors use locally
- devcontainer validation should be treated as a **supplemental** concern

This keeps the main merge gate:

- easier to understand
- faster to execute
- simpler to debug
- closer to GitHub's standard guidance
- less coupled to container-specific shell/runtime quirks

## Goals

This PBI should result in a maintainable baseline CI contract that:

1. validates restore/build health for `ViajantesTurismo.slnx`
2. runs the relevant automated test suite using the repository's xUnit v3 + Microsoft.Testing.Platform setup
3. runs repository quality checks that are part of the agreed baseline, such as `npm run lint:all`
4. publishes useful diagnostics and artifacts when failures occur
5. documents how contributors can reproduce CI locally
6. leaves room for future expansion without over-engineering the first workflow

## Non-Goals

This PBI does **not** attempt to deliver:

- release or deployment automation
- environment-specific CD approvals or secret-management flows
- a large OS/runtime matrix without a concrete requirement
- mandatory coverage thresholds before the underlying coverage workflow is stabilized
- a full devcontainer image publishing lifecycle
- a wholesale replacement of local developer workflows
- a container-only CI strategy for the default merge gate

## Proposed Change

Add one or more GitHub Actions workflows under `.github/workflows/` that establish a high-signal baseline CI path.

### Baseline workflow responsibilities

The baseline workflow should:

- restore .NET dependencies for the solution
- build `ViajantesTurismo.slnx`
- install Node dependencies with `npm ci`
- run repository quality checks using the current authoritative npm scripts
- run automated tests using the repository's supported `dotnet test` command shape
- upload relevant results and diagnostics as artifacts
- use workflow concurrency to cancel superseded runs on the same branch or PR

### Triggers

At minimum, CI should run on:

- `pull_request` targeting `main`
- `push` to `main`

It should also consider:

- `workflow_dispatch` for manual reruns or troubleshooting

### Tooling and setup requirements

The workflow should use repository-aligned tool versions and inputs:

- .NET SDK from `global.json`
- Node version aligned with `package.json` engines
- deterministic npm installation via `npm ci`
- built-in dependency caching from `actions/setup-dotnet` and `actions/setup-node` where it adds value
- least-privilege `permissions` for the `GITHUB_TOKEN`, defaulting to the minimum needed for baseline CI
- a documented policy for trusted GitHub Actions dependencies, including how versions are pinned and updated

### Suggested job layout

Start with a small number of clearly named jobs. For example:

1. **`build-and-test`**
    - checkout
    - setup .NET
    - setup Node
    - restore dependencies
    - build the solution
    - run tests
    - collect logs/results/artifacts

2. **`lint`**
    - checkout
    - setup Node
    - install npm packages with `npm ci`
    - run `npm run lint:all`

3. **Optional `devcontainer-smoke`**
    - run only when `.devcontainer/**` or closely related bootstrap files change
    - validate that the devcontainer builds successfully
    - optionally run one lightweight smoke command inside the container

The first implementation should favor clarity over cleverness. A small number of high-signal jobs beats a sprawling
maze of conditional logic with trust issues.

### Security and governance constraints

The baseline workflow should also establish a small but explicit security and governance contract:

- default `GITHUB_TOKEN` permissions should be restricted to the minimum required, such as `contents: read` unless a
  job needs more
- official GitHub-maintained actions should be preferred where possible
- third-party actions or workflows, if introduced, should follow a documented pinning/update strategy
- job names should be stable, human-readable, and unique so they can safely become required status checks
- workflow files should be protected by repository governance such as `CODEOWNERS` review where practical
- caches and artifacts must not contain credentials, tokens, or other secret-bearing files

### Baseline command shape

Unless and until the canonical script layer from PBI-2026-03-15-01 is in place, the baseline workflow should prefer a
clear restore/build/test separation using the repository's current authoritative commands. For example:

- `dotnet restore ViajantesTurismo.slnx`
- `dotnet build ViajantesTurismo.slnx --no-restore`
- `dotnet test --solution ViajantesTurismo.slnx --no-build`

This keeps failures easier to interpret and avoids unnecessary duplicate work.

## Devcontainer Positioning

The repository already contains a devcontainer with .NET 10, Node 24, and additional setup for containerized local
workflows. That environment is valuable, but it should not automatically become the primary CI execution model.

Recommended stance:

- **Use standard hosted runners** for the routine PR gate.
- **Use devcontainer validation selectively** for environment parity checks.
- **Consider prebuilding or publishing devcontainer images later** only if startup cost or reuse justifies it.

If a devcontainer-related workflow is added, its purpose should be explicit: protect the developer environment from
configuration drift without slowing every ordinary code change.

## Artifact and Diagnostics Expectations

CI should publish artifacts that help contributors debug failures without having to reproduce everything locally first.
Examples include:

- test result files and logs
- coverage outputs, if collected
- build logs or diagnostic summaries for failed runs
- any other lightweight troubleshooting outputs that materially improve diagnosis

Artifacts should:

- be uploaded with `if: always()` where appropriate
- have explicit retention settings
- be scoped to useful outputs rather than dumping the kitchen sink into storage
- use stable names that make sense in the Actions UI

Likely baseline artifact candidates for this repository include:

- `**/TestResults/**`
- `tests/**/TestResults/**/coverage.cobertura.xml` when coverage collection is enabled
- any aggregated coverage report directory if coverage-report generation is added later
- focused build or failure diagnostics generated specifically to support debugging

## Documentation Expectations

The CI contract should be documented in repository docs so contributors can answer the following without detective
work:

- what runs on every PR
- what must pass before merge
- which local commands correspond to CI checks
- how to reproduce failures locally
- how to extend the pipeline when new projects or checks are added
- when to use standard runner jobs versus the supplemental devcontainer path

Likely documentation touchpoints include `README.md`, `docs/CODE_QUALITY.md`, or a dedicated CI-focused document if
that becomes clearer.

Documentation should also clarify recommended repository governance that sits around CI, such as:

- which status checks are intended to become required before merge
- whether `.github/workflows/**` should be code-owner protected
- how workflow dependencies are updated and reviewed over time

## Acceptance Criteria

- A GitHub Actions workflow exists under `.github/workflows/` and runs automatically for pull requests and pushes to
  `main`.
- CI restores and builds the solution using the repository's pinned SDK and toolchain expectations.
- CI runs the relevant automated tests and fails when tests fail.
- CI runs repository quality checks appropriate for the agreed baseline scope.
- Baseline CI runs on a standard GitHub-hosted runner using `actions/setup-dotnet` and `actions/setup-node` rather
  than requiring the devcontainer for every PR.
- Node dependencies are installed deterministically with `npm ci`.
- Repository-aligned caching is used where it provides clear value without obscuring correctness.
- Workflow-level or job-level `GITHUB_TOKEN` permissions are explicitly restricted to the minimum required for baseline
  CI.
- Workflow configuration is clearly named, maintainable, and committed to the repository.
- Workflow job names are stable and unique so they can safely serve as required status checks.
- Contributors can identify which local commands correspond to CI validation.
- CI publishes named, actionable logs and/or artifacts with explicit retention so failures are diagnosable.
- Workflow concurrency is configured to cancel superseded runs when appropriate.
- The workflow avoids unnecessary duplication of repository scripts and is positioned to consume the canonical script
  layer as it evolves.
- The workflow follows a documented policy for trusted action dependencies and their updates.
- If a devcontainer validation job is included, it is supplemental rather than the primary build/test gate.

## Out of Scope

- Full deployment and release automation
- Production or environment-specific secrets orchestration
- Broad platform matrices without a demonstrated need
- Mandatory coverage thresholds from day one
- Replacing local developer workflows with CI-only execution
- Running the entire baseline CI suite inside the devcontainer by default
- Full devcontainer image publishing unless later justified

## Implementation Notes

- Start with one baseline workflow before splitting into many specialized pipelines.
- Prefer official GitHub-maintained actions for checkout, setup, artifact upload, and caching.
- Keep branch and path filtering simple at first; over-optimized filters often hide breakages.
- Prefer setup-action caching before introducing custom cache-key logic.
- Treat caches as performance optimizations only; do not cache credentials, tokens, or other sensitive files.
- Respect repository command shapes, especially around Microsoft.Testing.Platform and test-host options.
- Prefer an explicit restore → build → test flow with `--no-restore` / `--no-build` where appropriate.
- Use workflow or job `permissions` to implement least-privilege `GITHUB_TOKEN` access.
- Use explicit job and step names so failures are understandable from the Actions UI.
- Keep job names unique across workflows if they may become required checks under branch protection.
- Consider adding `CODEOWNERS` coverage for `.github/workflows/**` and related governance files so workflow edits are
  reviewed intentionally.
- If action pinning by full SHA is not adopted immediately, document the repository's update and trust policy for
  action versions.
- If container jobs are introduced later, document shell/runtime differences clearly.
- If a devcontainer smoke path is introduced later, consider `devcontainers/ci` or a Dev Container CLI-based approach
  rather than inventing bespoke container orchestration logic.
- After PBI-2026-03-15-01 lands, consolidate any duplicated command details behind the canonical script layer.

## Suggested Delivery Sequence

1. Inventory the minimum validation set required for PR safety: build, tests, and quality checks.
2. Create the initial workflow with hosted-runner setup for .NET and Node.
3. Verify the workflow uses the repository's documented commands and tool versions.
4. Add built-in caching, artifact upload, and concurrency control.
5. Add a targeted devcontainer smoke job only if justified and scoped.
6. Document the CI contract in repository docs.
7. Revisit the workflow once the canonical script layer is available so CI consumes shared commands rather than
   duplicating them.

## Planned Follow-Up Work

The baseline CI rollout is complete. The remaining planned follow-up work is:

- align branch protection so `SonarCloud` is required on `main`
- migrate GitHub Actions references from major tags to immutable SHAs when the stricter supply-chain posture is adopted
- add scheduled devcontainer smoke validation if environment-drift protection becomes necessary
- add a multi-OS matrix only when there is a concrete cross-platform requirement
- introduce coverage thresholds only after baseline coverage trends are stable

## Actionable Delivery Plan

Translate the suggested sequence above into a small set of implementation steps with clear outputs and verification.

### Phase 1 - Confirm the baseline contract

Scope the first workflow to the repository's currently documented validation surface and avoid expanding the first
delivery into release automation or an unnecessary matrix.

- Confirm the workflow uses the existing authoritative commands:
    - `dotnet restore ViajantesTurismo.slnx`
    - `dotnet build ViajantesTurismo.slnx --no-restore`
    - `dotnet test --solution ViajantesTurismo.slnx --no-build`
    - `npm ci`
    - `npm run lint:all`
- Confirm repository-pinned tool expectations before authoring workflow YAML:
    - .NET SDK from `global.json`
    - Node and npm engines from `package.json`
    - Microsoft.Testing.Platform runner configuration from `global.json`
- Confirm the initial artifact targets that are safe and useful to publish:
    - `**/TestResults/**`
    - focused build or failure diagnostics created during the workflow
- Decide whether the first increment includes only the baseline workflow, or baseline workflow plus governance docs.

**Output:** an agreed baseline scope that matches current repository commands and excludes non-goals.

**Verification:** the planned workflow steps can be mapped one-to-one to existing local commands in `README.md` and
`docs/CODE_QUALITY.md`.

### Phase 2 - Implement the baseline workflow

Create the first repository-owned workflow under `.github/workflows/` using standard hosted runners and official setup
actions.

- Add `.github/workflows/ci.yml` with triggers for:
    - `pull_request` targeting `main`
    - `push` to `main`
    - `workflow_dispatch`
- Add top-level `permissions` using least privilege, starting with `contents: read` unless a specific job proves it
  needs more.
- Add workflow `concurrency` so superseded runs on the same branch or PR are cancelled.
- Create a stable `build-and-test` job that:
    - checks out the repository
    - provisions .NET from `global.json`
    - provisions Node with npm caching
    - restores the solution
    - builds the solution with `--no-restore`
    - runs tests with `--no-build`
    - uploads test and diagnostic artifacts with `if: always()`
- Create a stable `lint` job that:
    - checks out the repository
    - provisions Node with npm caching
    - installs dependencies with `npm ci`
    - runs `npm run lint:all`
- Keep job names human-readable and unique so they can safely become required branch-protection checks.

**Output:** a working baseline CI workflow committed under `.github/workflows/`.

**Verification:** the YAML is readable, uses pinned repository tool versions, and does not duplicate logic beyond the
current documented command surface.

### Phase 3 - Add diagnostics and trust controls

Make failures actionable and ensure the workflow itself follows a simple governance model.

- Upload named artifacts with explicit retention values.
- Keep artifact scope narrow to logs and test outputs that materially help diagnosis.
- Prefer official GitHub-maintained actions for checkout, tool setup, and artifact upload.
- Document the repository policy for action references:
    - whether actions are pinned to major versions initially or immutable SHAs immediately
    - how those references are reviewed and updated over time
- Decide whether to include workflow governance in this increment by either:
    - adding a new `CODEOWNERS` file, or
    - extending an existing `CODEOWNERS` file, if one exists later,
        to require review for `.github/workflows/**`

**Output:** actionable failure diagnostics and an explicit trust/update policy for workflow dependencies.

**Verification:** a failed run still publishes the expected artifacts, and the governance decision is documented in a
repository-owned file.

### Phase 4 - Document the CI contract

Update repository documentation so contributors know what runs in CI and how to reproduce it locally.

- Update `README.md` to describe:
    - what the baseline CI workflow validates
    - the local commands that correspond to CI
    - where to find CI failures and artifacts in GitHub Actions
- Update `docs/CODE_QUALITY.md` or add a dedicated CI document to cover:
    - the workflow's job structure
    - artifact expectations
    - the stance on hosted runners versus supplemental devcontainer validation
    - the action trust and update policy
- Document the intended required status check names once the workflow file is finalized.

**Output:** repository docs that make the CI contract visible and reproducible.

**Verification:** a contributor unfamiliar with the pipeline can identify the required local commands and the expected
GitHub Actions checks without asking for tribal knowledge.

### Phase 5 - Validate and roll out safely

Treat the first run of the new workflow as a productionizing step, not just a YAML syntax exercise.

- Run the local equivalents of the CI commands before or while authoring the workflow:
    - `dotnet build ViajantesTurismo.slnx`
    - `dotnet test --solution ViajantesTurismo.slnx`
    - `npm run lint:all`
- Open a branch or test pull request to verify:
    - triggers fire as expected
    - job names appear exactly as intended
    - concurrency cancels superseded runs cleanly
    - artifact upload works on both success and failure paths where intended
- After one successful representative run, align branch protection with the finalized job names.
- Capture any deferred items as follow-up backlog work instead of stretching the first workflow beyond baseline CI.

**Output:** a validated workflow ready to become the shared merge gate.

**Verification:** at least one representative GitHub Actions run completes successfully and produces the expected job
names, logs, and artifacts.

### Concrete file checklist

The first implementation should likely touch only a small set of files.

- `.github/workflows/ci.yml` - baseline build, test, and lint workflow
- `README.md` - contributor-facing CI summary and local reproduction commands
- `docs/CODE_QUALITY.md` or a dedicated CI document - operational details, artifact guidance, trust policy
- `CODEOWNERS` - optional but recommended governance coverage for workflow files

### Atomic, testable implementation checklist

Break the delivery into tiny changes that each have a clear success signal. The goal is that each item can be
implemented, reviewed, and validated without needing the entire CI system to land at once.

#### Workflow skeleton

- [x] Create `.github/workflows/ci.yml` with only `name` and triggers for `pull_request`, `push`, and
    `workflow_dispatch`.
    - **How to test:** validate YAML structure locally and confirm the workflow appears in GitHub Actions on the next
        push.
- [x] Add top-level `permissions` with the minimum baseline scope.
    - **How to test:** confirm the workflow still starts successfully and does not request write permissions.
- [x] Add top-level `concurrency` with cancellation for superseded branch or PR runs.
    - **How to test:** push two quick successive commits to the same branch and confirm the earlier run is cancelled.

#### Build-and-test job foundation

- [x] Add a `build-and-test` job with `runs-on` and a checkout step only.
    - **How to test:** confirm the job starts and the repository contents are available to later steps.
- [x] Add .NET setup using `global.json`.
    - **How to test:** print or verify the installed SDK version during the run and confirm it matches
        `10.0.100`.
- [x] Add Node setup aligned with `package.json` engines.
    - **How to test:** print or verify the Node version during the run and confirm it is within the supported
        `>=24.0.0 <25.0.0` range.
- [x] Add solution restore: `dotnet restore ViajantesTurismo.slnx`.
    - **How to test:** the step exits successfully and later build steps can use `--no-restore`.
- [x] Add solution build: `dotnet build ViajantesTurismo.slnx --no-restore`.
    - **How to test:** the build step passes independently after restore and fails if compilation breaks.
- [x] Add solution test: `dotnet test --solution ViajantesTurismo.slnx --no-build`.
    - **How to test:** the test step passes on a healthy branch and fails the job if a test fails.

#### Artifact publishing

- [x] Add artifact upload for `**/TestResults/**` using `if: always()`.
    - **How to test:** confirm artifacts are uploaded on a successful run and still upload when tests fail.
- [x] Add explicit artifact naming and retention.
    - **How to test:** confirm the artifact name is stable in the Actions UI and retention is shown as expected.
- [ ] Add a small, focused diagnostic output if build or test fails.
    - **How to test:** induce a failing run and confirm the diagnostic files are present and useful.
    - **Status note:** deferred as follow-up work after baseline rollout completion.

#### Lint job foundation

- [x] Add a separate `lint` job with checkout and Node setup.
    - **How to test:** confirm the job starts independently from `build-and-test`.
- [x] Add deterministic package installation with `npm ci`.
    - **How to test:** the step succeeds using `package-lock.json` and fails if the lockfile or manifest is invalid.
- [x] Add `npm run lint:all`.
    - **How to test:** the job passes on a clean branch and fails when markdown, JSON, shell, or Gherkin linting is
        broken.

#### Caching and performance-safe improvements

- [x] Enable supported built-in caching for Node setup.
    - **How to test:** confirm cache restore/save behavior appears in the job logs and does not change correctness.
- [ ] Enable supported built-in caching for .NET setup if it provides clear value.
    - **How to test:** confirm cache activity appears in logs and the workflow still succeeds from a cold cache.

#### Documentation changes

- [x] Update `README.md` with a short CI summary and the local commands that correspond to CI.
    - **How to test:** a contributor can follow the documented commands locally without needing extra context.
- [x] Update `docs/CODE_QUALITY.md` or add a dedicated CI document describing jobs, artifacts, and failure
    reproduction.
    - **How to test:** the docs mention the actual workflow/job names and match the committed YAML.
- [x] Document the intended required status checks once job names are final.
    - **How to test:** the documented names exactly match the job names shown in GitHub Actions.

#### Governance and trust policy

- [x] Document the action versioning policy for the workflow.
    - **How to test:** reviewers can identify whether actions are pinned by major tag or immutable SHA and how updates
        are handled.
- [x] Add or update `CODEOWNERS` coverage for `.github/workflows/**` if governance is included in this increment.
    - **How to test:** a pull request changing workflow files requests the expected reviewers.

#### Optional supplemental work

- [ ] Add a `devcontainer-smoke` job only if there is a clear reason to validate `.devcontainer/**` changes now.
    - **How to test:** restrict execution to relevant changes and confirm it does not become the default merge gate.

#### Suggested PR slicing

If the work is split across multiple pull requests, prefer slices like these:

1. workflow skeleton, triggers, permissions, and concurrency
2. `build-and-test` job through restore and build
3. test execution and artifact upload
4. `lint` job and caching
5. documentation and governance updates
6. optional devcontainer smoke validation

Each slice should keep the repository in a releasable state and produce a visible, testable outcome in GitHub Actions.

#### Recommended pull request sequence

Use the following sequence if the work is implemented incrementally. Each pull request should be reviewable on its own
and leave the repository in a sensible intermediate state.

##### PR 1 - Bootstrap baseline workflow

**Goal:** establish the repository-owned CI entry point without taking on build/test responsibility yet.

- **Scope:** create `.github/workflows/ci.yml`, add workflow `name`, add triggers for `pull_request`, `push`, and
  `workflow_dispatch`, add least-privilege `permissions`, add `concurrency`, and add a minimal `build-and-test` job
  with checkout only.
- **Files:** `.github/workflows/ci.yml`.
- **Review focus:** workflow naming, trigger coverage, token permissions, and cancellation behavior.
- **Test signal:** the workflow appears in GitHub Actions, the single job starts successfully on a branch push or test
  PR, and a superseded run is cancelled as expected.

##### PR 2 - Add restore and build validation

**Goal:** turn the bootstrap workflow into a real compile gate while keeping test execution separate.

- **Scope:** add .NET setup from `global.json`, add Node setup only if already required by the job structure, add
  `dotnet restore ViajantesTurismo.slnx`, and add `dotnet build ViajantesTurismo.slnx --no-restore`.
- **Files:** `.github/workflows/ci.yml`.
- **Review focus:** toolchain provisioning, restore/build command shape, and failure clarity in the Actions UI.
- **Test signal:** the job fails on compilation problems and the build passes on a healthy branch using the pinned
  SDK.

##### PR 3 - Add test execution and baseline artifacts

**Goal:** promote the workflow from compile validation to real merge-gate validation.

- **Scope:** add `dotnet test --solution ViajantesTurismo.slnx --no-build`, add upload of `**/TestResults/**`, add
  explicit artifact name and retention, and add `if: always()` to artifact publishing.
- **Files:** `.github/workflows/ci.yml`.
- **Review focus:** test command compatibility with Microsoft.Testing.Platform, artifact usefulness and retention, and
  success/failure behavior.
- **Test signal:** tests fail the job when a test breaks and artifacts upload on both pass and fail paths where
  intended.

##### PR 4 - Add lint job and safe caching

**Goal:** add the non-.NET quality gate without entangling it with build-and-test execution.

- **Scope:** add a separate `lint` job, add Node setup aligned to `package.json`, add `npm ci`, add
  `npm run lint:all`, and enable built-in caching where it is straightforward and low risk.
- **Files:** `.github/workflows/ci.yml`.
- **Review focus:** job separation and naming, deterministic npm installation, and caching correctness over
  cleverness.
- **Test signal:** lint failures independently fail the `lint` job and cache log entries appear without changing
  correctness.

##### PR 5 - Document the CI contract

**Goal:** make the workflow understandable and reproducible for contributors and reviewers.

- **Scope:** update `README.md`, update `docs/CODE_QUALITY.md` or add a dedicated CI document, document local
  reproduction commands, and document intended required status check names.
- **Files:** `README.md` and `docs/CODE_QUALITY.md` or a new CI-focused document.
- **Review focus:** documentation accuracy, parity between docs and workflow names, and contributor usability.
- **Test signal:** a reviewer can map every documented command and check to the live workflow.

##### PR 6 - Add governance and trust policy

**Goal:** tighten the operational contract around workflow changes and action dependencies.

- **Scope:** document the action pinning/update policy and add or update `CODEOWNERS` for `.github/workflows/**` if
  desired in this increment.
- **Files:** `README.md`, `docs/CODE_QUALITY.md`, or dedicated CI governance documentation, plus `CODEOWNERS` if
  introduced.
- **Review focus:** trusted action policy, ownership of workflow changes, and maintainability of the update process.
- **Test signal:** the policy is explicit in repo docs and workflow changes request the expected reviewers when
  `CODEOWNERS` is active.

##### PR 7 - Optional devcontainer smoke validation

**Goal:** protect the devcontainer path without making it the default gate for ordinary changes.

- **Scope:** add `devcontainer-smoke` only if justified, restrict it to relevant paths or specific scenarios, and keep
  it supplemental to the hosted-runner baseline.
- **Files:** `.github/workflows/ci.yml` or a separate supplemental workflow file.
- **Review focus:** trigger scope, runtime cost, and separation from baseline CI.
- **Test signal:** the job runs only when intended and does not slow routine PR validation unnecessarily.

##### Good defaults for PR metadata

If the team wants consistency, use lightweight labels or prefixes such as:

- `ci` - workflow behavior changes
- `documentation` - README or quality docs updates
- `governance` - `CODEOWNERS`, policy, or branch-protection guidance
- `devcontainer` - environment parity validation changes

Example PR titles:

- `ci: add baseline GitHub Actions workflow skeleton`
- `ci: add restore and build validation to baseline workflow`
- `ci: run solution tests and publish test result artifacts`
- `ci: add lint job with deterministic npm install`
- `docs: document baseline CI contract and local reproduction`
- `governance: define workflow ownership and action update policy`

### Recommended sequencing constraints

Keep the first PR intentionally boring in the best possible way.

- Do not add a multi-OS matrix unless a concrete requirement appears.
- Do not make the devcontainer the default execution path.
- Do not block the initial workflow on coverage thresholds.
- Do not introduce third-party actions unless the benefit is clear and the trust/update policy is documented.
- Do not optimize with path filters until the baseline pipeline has proven trustworthy.

### Exit criteria for implementation

The implementation can be considered ready for review when all of the following are true:

- the workflow file exists and triggers on PRs and pushes to `main`
- hosted-runner jobs restore, build, test, and lint using repository-aligned commands
- permissions, concurrency, and artifact upload are explicitly configured
- CI documentation is updated with local reproduction guidance
- any governance decision on action pinning and workflow review ownership is documented
- deferred work is captured separately instead of hidden in TODO comments or implied future magic

## Dependencies and Follow-Ups

### Reasonable follow-up work

Potential follow-up items after the baseline workflow is stable:

- coverage artifact/report generation refinement
- coverage badge wiring and threshold tuning after the hosted quality path is implemented
- promoting SonarCloud to a required branch-protection check after stable baseline runs
- branch protection guidance for required checks
- `CODEOWNERS` protection for `.github/workflows/**` and related automation assets
- Dependabot updates for GitHub Actions references and other CI-related dependency surfaces
- dependency review for pull requests that change manifests or lock files
- workflow-level security scanning or secret scanning additions where justified
- scheduled devcontainer validation
- selective path-based optimizations once the workflow behavior is proven
- reusable workflows or composite actions once duplication pressure justifies them
- optional split of fast checks versus heavier suites if runtime becomes a bottleneck

## Risks and Considerations

- **Workflow sprawl:** Avoid starting with too many jobs or matrices.
- **Hidden duplication:** Keep CI aligned with repo scripts and docs.
- **Environment drift:** Standard runner CI should validate the common path; devcontainer validation should protect the
  specialized path.
- **Artifact noise:** Upload enough to debug, not enough to recreate an archaeological dig.
- **Workflow supply chain risk:** Unreviewed action dependencies or broad token permissions can undermine otherwise
  healthy CI design.
- **False speed wins:** Aggressive path filters or brittle custom caches can make CI faster while making trust worse.

## Success Metrics

- Pull requests receive fast, reliable feedback on build, test, and quality regressions.
- Fewer broken changes reach `main`.
- Reviewers spend less time manually verifying routine merge safety.
- Contributors can reproduce CI failures locally with minimal ambiguity.
- CI remains easy to extend as new checks and projects are added.
- Devcontainer regressions are caught through targeted validation without slowing routine pull requests.

## Definition of Done

This PBI is done when:

- the workflow is checked in and triggers correctly
- the baseline jobs pass on a representative branch with expected artifacts/logs
- required documentation is updated to explain the CI contract and local reproduction commands
- the implementation is simple enough that a new maintainer can understand it from the Actions UI and repository docs

## Notes for Implementation

The guiding principle for this PBI is **high signal, low ceremony**. The first workflow should prove the merge gate,
not win an award for elaborate YAML origami.
