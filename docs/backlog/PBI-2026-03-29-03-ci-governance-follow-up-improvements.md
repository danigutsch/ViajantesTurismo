# PBI-2026-03-29-03 — CI governance follow-up improvements

## Metadata

- **ID:** PBI-2026-03-29-03
- **Title:** CI governance follow-up improvements
- **Priority:** Medium
- **Status:** Planned
- **Type:** Developer Experience / CI Governance / Repository Hygiene

## Goal

Capture the next practical improvements identified after the CI workflow consolidation work so
they can be reviewed, prioritized, and implemented in a small, low-risk sequence.

## Context

The baseline CI governance rollout is complete. The next improvements should focus on repository
maintainability and contributor experience rather than introducing another large workflow change.

The current best candidates are:

1. extend automated dependency updates to devcontainer-related inputs
2. add `.git-blame-ignore-revs` so future mechanical commits do not pollute blame history
3. document the devcontainer and Codespaces path more explicitly
4. keep larger CI topology changes deferred unless runtime or maintenance pain justifies them

## Proposed plan

### 1. Expand Dependabot coverage for devcontainer inputs

#### Why

The repository already has a `.devcontainer/` setup, but automated dependency update coverage is
currently limited to `github-actions`, `nuget`, and `npm` in `.github/dependabot.yml`.

#### Work

- add a `devcontainers` update entry to `.github/dependabot.yml`
- evaluate whether `docker` updates also make sense for this repository
- keep grouping, schedule cadence, and PR limits aligned with the current low-noise strategy
- document the added ecosystem coverage in `docs/ci/overview.md` if adopted

#### Acceptance criteria

- Dependabot checks devcontainer inputs automatically
- the schedule and grouping do not create excessive PR churn
- documentation reflects the extra ecosystem coverage if it is enabled

### 2. Add `.git-blame-ignore-revs`

#### Why

Large mechanical commits such as formatting, generated-file refreshes, markdown wrapping, or
workflow normalization are often useful changes, but they make `git blame` less informative.

#### What `.git-blame-ignore-revs` does

`.git-blame-ignore-revs` is a plain text file that lists commit SHAs Git should ignore when blame
is calculated.

When Git is configured to use that file, blame skips those commits and attributes the affected
lines to the previous non-ignored revision instead.

This means:

- it **does help** keep blame useful after mechanical commits
- it **does not** rewrite history
- it **does not** remove commits from `git log`
- it **does not** change diffs, merges, or repository contents
- it only affects how blame attribution is displayed by tools that honor the file

Typical candidates for the file are commits that:

- apply formatting only
- rewrap markdown without semantic changes
- refresh generated files in bulk
- normalize whitespace or line endings
- perform other intentionally mechanical edits

#### Work

- add a root `.git-blame-ignore-revs` file
- decide which historical commits are safe to mark as mechanical
- avoid adding commits with meaningful logic changes to the ignore list
- document local usage expectations for contributors if needed

#### Acceptance criteria

- `.git-blame-ignore-revs` exists at the repository root
- only clearly mechanical commits are added to it
- the file improves blame usability without hiding meaningful authoring history

### 3. Document the devcontainer and Codespaces path

#### Why

The repository already contains a devcontainer, but the contributor path is still more implied than
explicit. A short, maintained guide would reduce setup friction and make future automation easier to
justify.

#### Work

- document how to open and validate the devcontainer locally
- document whether GitHub Codespaces is supported, experimental, or simply unoptimized
- define the minimum validation steps for this path
- decide whether a future Codespaces prebuild workflow is worth considering

#### Acceptance criteria

- contributors have a single documented entry point for devcontainer usage
- expectations for local validation are clear
- future Codespaces work has an explicit baseline instead of guesswork

### 4. Keep CI topology changes deferred unless a clear pain signal appears

#### Why

Other repositories use more advanced dependency-aware CI lane splitting and final results jobs, but
that extra structure is only worth adopting if the current workflow becomes slow, noisy, or hard to
reason about.

#### Work

- monitor current CI duration and failure-investigation friction
- revisit deeper job splitting only if there is a measurable bottleneck
- prefer targeted optimization over premature workflow abstraction

#### Acceptance criteria

- any future CI restructuring is driven by real evidence
- current workflow clarity is preserved unless there is a strong reason to trade it away

## Recommended execution order

1. update Dependabot coverage for `devcontainers`
2. add `.git-blame-ignore-revs`
3. document the devcontainer and Codespaces path
4. revisit deeper CI topology changes only if operational pain emerges

## Out of scope for this follow-up item

The following are intentionally not part of the immediate plan:

- large CI rewrites with reusable workflow layers everywhere
- flaky-test quarantine automation before flaky tests become a recurring problem
- artifact-heavy PR dogfooding scripts similar to product/tooling repositories
- service-by-service container publishing unless the application architecture demands it

## Suggested next step

Create a small implementation PR that updates `.github/dependabot.yml` and adds the root
`.git-blame-ignore-revs` file, then follow with documentation updates in a second pass if the
first change lands cleanly.
