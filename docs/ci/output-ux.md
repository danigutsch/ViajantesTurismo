# CI output UX

Research baseline and recommended direction for making CI output easier to consume by
humans and machines.

## Why this matters

The current CI workflow already provides:

- required-check level job names
- per-slice artifacts
- GitHub step summaries for skip paths and SonarCloud
- phase timing files for slices and SonarCloud

That is a solid base, but the output model is still split across:

- raw step logs
- GitHub job summaries
- uploaded artifacts
- ad hoc shell output patterns

This makes it harder to answer the most important questions quickly:

- What failed?
- What was skipped, and why?
- Where should a human look next?
- Which file or artifact should a tool consume first?

## Official GitHub Actions guidance

This repository should prefer the patterns documented by GitHub Actions itself.

### 1. Put important run information in job summaries

GitHub documents `GITHUB_STEP_SUMMARY` as the right place to display grouped,
workflow-run-visible information so people do not need to open logs just to see the key
result of a run.

Implication for this repo:

- summaries should carry the high-signal status and next actions
- logs should remain the detailed trace, not the primary status surface

### 2. Use grouped logs for readable step output

GitHub documents `::group::` and `::endgroup::` as the standard way to make logs
expandable and easier to scan.

Implication for this repo:

- phase-level shell output should be grouped consistently
- the default visible console surface should emphasize phase boundaries, not raw command
  chatter

### 3. Use annotations for actionable warnings and errors

GitHub documents `::notice::`, `::warning::`, and `::error::` as the canonical way to
surface actionable messages in the workflow UI.

Implication for this repo:

- only actionable items should become annotations
- informational progress output should generally stay in logs or summaries
- not every line that contains the word "warning" should become a GitHub warning

### 4. Use artifacts for detailed debugging output

GitHub documents artifacts as the mechanism for retaining build and test outputs and for
sharing data between jobs.

Implication for this repo:

- verbose, failure-oriented detail belongs in artifacts more than in the always-visible
  console stream
- artifacts should have stable names and predictable content so humans and tools can find
  the right file quickly

### 5. Logs are still important, but they are not the best high-level status surface

GitHub's log guidance emphasizes viewing failed steps, searching logs, and downloading
archives, which is useful, but still a lower-level troubleshooting path than summaries or
annotations.

One important practical note from GitHub's log UI:

- searching logs only covers expanded steps

Implication for this repo:

- critical outcome data should not rely on deep log search alone
- summaries and annotations should carry the first-pass diagnosis path

## Current repository baseline

### Human-facing surfaces

- skip steps append short summary notes to `GITHUB_STEP_SUMMARY`
- `scripts/run-ci-test-slice.sh` appends a small timing table for each slice
- `scripts/write-github-sonar-summary.sh` emits the richest current summary:
    - validation outcome
    - quality gate status
    - SonarCloud link
    - warning count
    - phase timings
    - parse-warning section
    - next-place-to-look guidance on failure

### Machine-facing surfaces

- slice timing files: `TestResults/<slice>-phase-timings.tsv`
- Sonar timing file: `TestResults/ci-phase-timings.tsv`
- per-slice test results artifacts
- per-slice diagnostics artifacts on failure
- Sonar coverage and Sonar log artifacts

### Current gaps

- timing schemas are inconsistent between slice and Sonar outputs
- there is no single machine-readable CI manifest for the whole run
- summaries are uneven: Sonar is rich, slice summaries are sparse
- diagnostics are useful but not yet normalized for machine consumption
- console output still mixes progress output and diagnostic output in a mostly free-form way

## Recommendation for this repository

Do **not** jump directly to a blanket "warnings/errors only in normal output" policy.

That may reduce noise, but it can also make debugging harder if the detailed execution path
disappears from the visible log stream. The better sequence is:

1. Normalize the output contract.
2. Improve the high-signal summary surfaces.
3. Decide what low-value console output can be demoted.

## Recommended target model

### Human-first surfaces

The GitHub UI should show, without opening artifacts:

- whether a slice ran, skipped, or failed
- the top-level failure reason
- duration by phase
- the next artifact or log to inspect

### Machine-first surfaces

Each CI job should emit stable, low-ambiguity files for tooling consumption.

Recommended minimum:

- one normalized timing schema across all CI helpers
- one run-manifest file per job that records:
    - job name
    - phase names
    - status
    - skip reason if any
    - primary log file
    - primary artifact names
    - duration

JSON is the best likely target for machine consumption. TSV remains acceptable for narrow,
append-only timing data if the schema is kept consistent.

## Suggested implementation order

### Phase 1: Normalize existing outputs

- align slice and Sonar timing schemas
- standardize failure summary sections across slice and Sonar helpers
- standardize "next place to look" guidance

### Phase 2: Add a machine-readable manifest

- emit a small per-job manifest file
- upload it with diagnostics or test-results artifacts as appropriate
- document the manifest as the preferred automation input

### Phase 3: Tighten console verbosity

- keep phase headers and critical progress visible
- keep actionable warnings/errors as annotations
- move high-volume informational detail to log files and artifacts when it adds little value
  to the live console stream

## Specific guidance on log level reduction

The idea of reducing the regular console stream to mostly warnings/errors is worth
researching, but should be treated as a later optimization, not the first change.

Recommended default policy:

- console:
    - phase start/end
    - actionable warnings/errors
    - clear next-step guidance on failure
- summaries:
    - status, timings, links, and human-readable diagnosis hints
- artifacts/log files:
    - verbose info-level detail
    - full command output
    - machine-readable manifests and timing files

That model follows GitHub's intended split more closely than trying to make the console log
serve every audience at once.

## Official source references

- GitHub Actions workflow commands and job summaries:
  `https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands`
- GitHub Actions artifacts:
  `https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts`
- GitHub Actions workflow run logs:
  `https://docs.github.com/en/actions/monitoring-and-troubleshooting-workflows/using-workflow-run-logs`
