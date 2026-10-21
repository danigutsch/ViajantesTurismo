# CI throughput and rollout

The application and library keep their existing runner capacity. Both repositories remain under
the personal account; GitHub's native merge queue requires organization ownership, so application
strict up-to-date checks remain enabled. SharedKernel validation remains advisory.

## Validation ownership

Application selection, slice preparation, build evidence, dependency-review planning, and PR title
validation live in `ViajantesTurismo.RepoConfig.Tool`. The existing coverage collector and browser
installer remain platform adapters. GitHub Actions owns scheduling, permissions, caching, and
artifact transport. Required source-validation names remain stable.

`Solution Build` runs alongside test slices and uploads revision-bound Release PDB hashes. The
coverage job downloads and verifies the symbols rather than rebuilding the whole application.
Missing, changed, stale, or undeclared symbols fail validation. Portable-PDB document checks and
the 80% changed-code threshold remain unchanged.

Library repository tooling selects and builds project graphs. Candidate tooling creates each
hashed package set once; Linux and Windows independently consume those same bytes and validate
NativeAOT. Documentation-only candidate selection already existed before this change and remains
conservative for package READMEs and uncertain build inputs.

## Documentation-only validation

Documentation changes skip application builds, test matrices, Windows tests, version calculation,
and repository-tool test runs on both pull requests and main pushes. Selection restores only the
standalone repository tool. Markdown lint, local link validation, line endings, metadata, and secret
scanning remain active, and required aggregate checks still report a result.

The documentation allowlist covers Markdown and image files under `docs/` plus root `README.md`,
`AGENTS.md`, `CONTRIBUTING.md`, and `SECURITY.md`. Package READMEs, scripts, unknown extensions,
build inputs, and missing comparisons retain their applicable validation. Low-risk contributor
scripts can skip application tests while retaining the tooling tests in repository lint.

Main pushes compare the exact before/after revisions. Pull requests compare against their merge
base. Manual and merge-group runs retain full selection; invalid comparisons fall back to full
validation. Do not use commit-message skip annotations or workflow-level path filters for required
checks, because those can leave merge gates pending.

## Dependency-review diagnosis

Run `39600350445` took 19m 04s overall. Its review step took 13m 10s: approximately 2m 07s passed
before the comparison reported unequal snapshot counts, followed by approximately 10m 58s before
vulnerability/license summaries appeared. The log contains a very large dependency diff and
informational Scorecard output. These timestamps do not isolate license lookup from Scorecard
lookup, and they do not prove snapshot retrying was the cause. The pinned action defaults snapshot
retrying to false. Keep vulnerability/license enforcement and preview approvals unchanged.

The initial optimization skips graph submission/review only for recognized standalone documentation
changes. It does not skip source changes or reuse another revision's snapshot. Broader skipping
requires evaluated restore-input evidence; unknown input and comparison failures retain full review.

## Historical baseline

The following sample was collected on October 21, 2026 from the latest 100 workflow runs in each
repository. Each row uses the latest ten completed runs of that workflow. Elapsed duration is
`updated_at - created_at`, including queueing and postprocessing. Median and nearest-rank p95 use
successful runs only. Small, mixed samples are diagnostic rather than a performance guarantee.

| Workflow | Completed sample | Successful | Median | p95 | Cancelled |
| --- | ---: | ---: | ---: | ---: | ---: |
| Application CI | 10 | 7 | 504s | 720s | 2 |
| Dependency Review | 10 | 4 | 685s | 2338s | 4 |
| Secret Scan | 10 | 10 | 29.5s | 36s | 0 |
| SharedKernel validation | 10 | 8 | 1001.5s | 1440s | 2 |

For rollout comparison, separate documentation, application source, library source, dependency,
and global-build changes. Compare runner wait, setup/restore, build, tests, package/AOT consumption,
artifact overhead, cancellations, and compute minutes. Compare equivalent scenarios, not just overall
medians. New slice manifests and library execution timings record local phases. Hosted timing after
rollout must be measured before claiming an improvement.

## First post-rollout sample

These source-validation runs are single observations, not equivalent workloads or a performance
claim. Application [main run 39670817591](https://github.com/danigutsch/ViajantesTurismo/actions/runs/39670817591)
completed in 430s: selection 56s, solution build 141s, longest test slice 288s, and quality gate 74s.
Library [main run 39670639814](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/39670639814)
completed in 385s: selection 33s, ordinary tests 248s, repository quality 301s, candidate creation 133s,
and Windows consumption 199s. Compare documentation runs separately after the fast path lands.

## Repository settings rollout

Applied on October 21, 2026 after successful main validation: both repositories use squash-only
merges with PR titles and bodies, automatic branch deletion, and a `ci:rerun` label. Application
`PR Metadata` is required after it passed on the existing payment PR. Strict updates, signed commits,
existing reviews/checks, and advisory SharedKernel validation remain unchanged.


Land and inspect successful workflow evidence before applying repository settings. Enable squash
only, use `PR_TITLE` for squash headers and `PR_BODY` for descriptions, and retain branch deletion.
Preserve application signing, strict up-to-date behavior, reviews, and existing required checks.
Add `PR Metadata` as an additional required application check only after that workflow reports on
active PRs; do not replace `Lint` or other source checks with metadata-only results. Keep SharedKernel
checks advisory and do not add branch protection there.

Create the `ci:rerun` label in both repositories. Adding it restarts PR-context validation; remove and
reapply for another attempt. Routine metadata edits do not cancel source-validation runs. Recovery
uses a human or authorized app token, because `GITHUB_TOKEN` label changes may not trigger workflows.

If rollout fails, revert the workflow change independently of package APIs, restore the prior merge
settings and required-check configuration, and retain immutable candidates and diagnostic artifacts.
No package-registry publication, stable tag, or consumer-pin change is part of this work.
