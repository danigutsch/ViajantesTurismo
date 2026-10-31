# Artifacts and local reproduction

This document describes the CI artifacts produced by the main validation workflow and the
local commands used to reproduce common failures.

## Artifacts

When CI executes a test-slice matrix row, it uploads its own `ci-test-slice-<id>-results` artifact with
`if: always()` so test outputs, coverage XML, and run evidence survive both passing and failing runs.
For documentation-only pull requests, the lightweight skip path runs instead and no slice
artifacts are produced.

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `ci-selection` | `TestResults/selection/**`: `selection.json` with reasons, slice solution filters, and `build.slnf` | 7 days |
| `ci-test-slice-<id>-results` | `**/TestResults/**` produced by one test-slice matrix row | 7 days |
| `solution-build-evidence` | `TestResults/symbol-evidence.json` and the Release PDBs under `src` | 7 days |
| `quality-evidence` | `TestResults/coverage-gate.json` for pull requests and merge groups | 14 days |
| `dependency-review-selection` | The Dependency Review plan's `TestResults/selection/**` | Repository default |

Each slice result artifact holds the execution directory `TestResults/selection/execution-<id>/` with
`phase-timings.tsv`, `manifest.json`, `diagnostics.txt`, one `<phase>.log` per phase, and the
per-project Cobertura reports under `results/<Project>/<tfm>_<rid>/`. `run` writes the evidence files
whether the run passes, fails, or is canceled.

For local validation, missing result files are treated as an error because that indicates
the test infrastructure did not produce the expected outputs. In CI, artifact upload is
best-effort (`if-no-files-found: ignore`), so missing result files do not by themselves
fail the workflow but should still be investigated.

Test slices publish Cobertura XML in their result artifacts. The secret-free `Quality Gate`
job reads those reports directly and checks changed-code coverage on pull requests and merge groups.

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

A failed slice also prints `dotnet --info` in the job log.

## Reproducing failures locally

All CI commands map directly to local developer commands.

### Test-slice matrix row

```bash
# From repository root
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
dotnet tool run sharedkernel-ci -- select
dotnet tool run sharedkernel-ci -- run --slice <slice-id> --coverage
```

`select` without `--base` selects every test project; pass `--base <commit>` to select what a change
since that commit affects. `run` restores, builds, and tests the slice's selected projects, generates
its OpenAPI targets, and records per-phase timings in `TestResults/selection/execution-<slice-id>/`.
Replace `<slice-id>` with a `slices[].name` of `.config/sharedkernel-ci.json`; add `--install-playwright`
for `hosted-tests`. The logical fast lane uses the `fast-validation-1` and `fast-validation-2` slices
and produces a separate result artifact for each shard.

To reproduce coverage aggregation and changed-code enforcement locally, use the commands in
[quality gates](quality-gates.md#changed-code-policy). No analysis credentials are needed.

For documentation-only pull requests (the `selection.documentation` patterns of
`.config/sharedkernel-ci.json`), CI skips the validation commands above. The affected test jobs use lightweight skip paths, and the required
`Build and Test` aggregate records their successful outcomes.

### Lint job

The Lint job runs these commands from the repository root after `setup-dev.sh` (or
`setup-dev.ps1`) stages the candidate; run the failing one to reproduce it:

Before these, the job runs `verify-action-pins` with the RepoConfig tool built from the shared-workflow SHA
against SharedKernel's `.github/action-pins.json`. To reproduce it, build that tool from a SharedKernel checkout
at the SHA and run `verify-action-pins --root . --manifest <checkout>/.github/action-pins.json`.

```bash
shellcheck -- .devcontainer/*.sh scripts/*.sh setup-dev.sh
dotnet tool run sharedkernel-repo -- verify-text-encoding --root .
dotnet tool run sharedkernel-repo -- verify-line-endings --root .
dotnet tool run sharedkernel-repo -- verify-json --root .
dotnet tool run sharedkernel-repo -- verify-links --root .
dotnet tool run sharedkernel-repo -- verify-gherkin --root .
dotnet tool run sharedkernel-repo -- verify --root .
dotnet tool run sharedkernel-docs -- verify --config docs/architecture/generated-diagrams.json
dotnet tool run sharedkernel-docs -- check --config docs/architecture/documentation-conformance.json
```
