# Artifacts and local reproduction

This document describes the CI artifacts produced by the main validation workflow and the
local commands used to reproduce common failures.

## Artifacts

When CI executes a test-slice matrix row, it uploads its own `ci-test-slice-<id>-results` artifact with
`if: always()` so test outputs and coverage XML survive both passing and failing runs.
For documentation-only pull requests, the lightweight skip path runs instead and no slice
artifacts are produced. Each row uploads a focused `ci-test-slice-<id>-diagnostics` artifact only when
its `Run test slice` step fails.

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `ci-selected-test-projects` | Generated project-level selection plan for every fixed test slice | 1 day |
| `ci-test-slice-<id>-results` | `**/TestResults/**` produced by one test-slice matrix row | 7 days |
| `quality-evidence` | Merged HTML/Cobertura, changed-code diff, and coverage-gate JSON when applicable | 14 days |
| `ci-test-slice-<id>-diagnostics` | Focused failure summary for one failed test-slice matrix row | 7 days |

The slice result artifacts also include machine-readable helper outputs such as
`*-phase-timings.tsv` and `*-manifest.json`, alongside uniquely named Cobertura XML files in a
run-specific `TestResults/coverage.*` directory when coverage collection is enabled.

For local validation, missing result files are treated as an error because that indicates
the test infrastructure did not produce the expected outputs. In CI, artifact upload is
best-effort (`if-no-files-found: ignore`), so missing result files do not by themselves
fail the workflow but should still be investigated.

The HTML coverage artifact is generated from those uniquely named Cobertura files with the
repo's local `reportgenerator` tool manifest entry.

Test slices publish Cobertura XML in their result artifacts. The secret-free `Quality Gate`
job merges those reports into HTML and Cobertura and checks changed-code coverage on pull
requests and merge groups. Full-run trends are reported separately from selected-slice coverage.

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

When a validation slice fails before full test artifacts are available, CI also uploads a
small `*-diagnostics` artifact containing step outcomes, toolchain versions, a
`TestResults` inventory snapshot, the captured phase timing tables, and any generated
manifest files to speed up first-pass diagnosis.

## Reproducing failures locally

All CI commands map directly to local developer commands.

### Test-slice matrix row

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Local test slice" \
  --projects-file scripts/ci-test-slices/<slice-id>.txt
```

`scripts/run-ci-test-slice.sh` is a post-restore helper. It builds single-project slices directly,
builds multi-project slices through one temporary solution, runs them with coverage, and records
per-slice timing information. Aggregated HTML coverage is generated once later by the `Quality Gate`
job. Replace `<slice-id>` with a file basename from `scripts/ci-test-slices`; add
`--install-playwright` only for a matrix row that enables it in `.github/workflows/ci.yml`.
The logical fast lane uses the `fast-validation-1` and `fast-validation-2` basenames and produces
separate result and diagnostic artifacts for each shard.

To reproduce coverage aggregation and changed-code enforcement locally, use the commands in
[quality gates](quality-gates.md#changed-code-policy). No analysis credentials are needed.

For documentation-only or low-risk contributor-maintenance pull requests (`docs/**`, `README.md`,
`CONTRIBUTING.md`, and the allowlisted files in the `select-ci-test-projects` command), CI skips the
validation commands above. The affected test jobs use lightweight skip paths, and the required
`Build and Test` aggregate records their successful outcomes.

### Lint job

```bash
# From repository root
bash scripts/lint-all.sh
```

If the CI lint job fails, run individual linters to isolate the failure:

```bash
bash scripts/lint-markdown.sh              # Markdown
shellcheck **/*.sh                         # Shell scripts
dotnet tool run sharedkernel-repo -- verify-json --root .
bash scripts/lint-gherkin.sh tests/**/*.feature  # Gherkin/feature files
```
