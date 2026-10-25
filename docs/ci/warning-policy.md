# CI warning policy

Owner: repository maintainers. Status: active classification policy. Reviewed October 20, 2026.
Workflow commands, `.editorconfig`, and `Directory.Build.props` determine current enforcement.
This policy classifies findings; it does not silently change tool severities or add summary automation.

## Contents

- [Classification and enforcement](#classification-and-enforcement)
- [Exceptions and baseline](#exceptions-and-baseline)
- [Review lifecycle](#review-lifecycle)

## Classification and enforcement

Classify by impact and confidence, not merely a tool's word "warning". A security, correctness,
or reliability failure is blocking when evidence is actionable. Cosmetic advice is not automatically
a blocker; an unknown or incomplete analysis is not evidence of success.

| Tool or signal | Blocking | Budgeted debt or advisory | Enforcement owner and current boundary |
| --- | --- | --- | --- |
| Build/compiler/analyzers | Build errors and unsuppressed warning diagnostics under warnings-as-errors; actionable correctness, security, or lifetime failures | Reviewed path/rule suppressions are debt; suggestion/info diagnostics remain visible advice | Application owner fixes findings; repository maintainers own `Directory.Build.props`, `.editorconfig`, and the required full-solution Quality Gate build |
| NuGet restore/audit | Failed restore, invalid lock graph, and diagnostics currently promoted by build/restore configuration; actionable high/critical vulnerability decisions require resolution or explicit reviewed exception | Lower-confidence/advisory diagnostics remain visible; selected audit-warning promotion is separate enforcement work, not assumed complete here | Dependency owner triages packages and transitive impact; CI maintainers own locked restores; current audit configuration is enabled, all dependencies, high threshold |
| Dependency Review | Actionable vulnerability/license failures selected by its workflow configuration | Findings outside its configured threshold require review; absence from a dependency diff is not a clean whole-tree audit | Repository/security maintainers own the required Dependency Review check and configuration |
| Secret scanning | Confirmed secret detections and failed required scanning execution | Reviewed false-positive allowlist entries are debt, never permission to retain a real secret | Security/repository maintainers own Secret Scan; application owner rotates/removes exposed credentials through the incident path |
| Lint | Nonzero required Markdown, links, JSON, Gherkin, shell, workflow, encoding, and generated-document checks | Optional spelling suggestions are advisory; unrelated existing debt needs an explicit baseline before it can be budgeted | Documentation/workflow owners fix their files; repository maintainers own Lint and Actionlint configuration |
| Tests | Failed tests, test-host crashes, missing expected test execution, and unreliable/corrupt results | Diagnostic log noise is advisory only after proving successful execution; flaky tests require owned remediation, not repeated reruns as acceptance | Suite owner fixes behavior; CI maintainers own Build and Test aggregation and result collection |
| Coverage | Changed-code coverage below the configured 80% combined executable-line/condition threshold, malformed evidence, or missing required instrumentation | Whole-tree trends are advisory; targeted exclusions require evidence and review | Application/test owners add meaningful coverage; CI maintainers own Quality Gate and evidence validation |
| Sonar, if reintroduced | Hosted quality-gate failures, actionable security/correctness/reliability failures, and unexpected scanner parse failures | Medium-impact maintainability findings stay visible and non-blocking unless the hosted gate fails; do not blanket-promote them | CI maintainers must define scanner-health and hosted-gate checks before adoption; Sonar is not part of current CI |

Current Sonar retirement and the limits of replacement analysis are documented in
[quality gates](quality-gates.md). Its older warning-policy issue predates that change.
Do not imply the local build reproduces Sonar cognitive-complexity, duplication, or impact metrics.
The conditional Sonar row preserves the requested maintainability distinction without reinstating
an account, scanner, secrets, or remote check.

Do not bypass required failures with `continue-on-error`, ignored exit codes, broad catch wrappers,
or unconditional successful aggregate checks. Optional advisory steps must be explicitly identified
and must not contain blocker work. Preserve fork safety and documentation-only exclusions;
"not run" must be distinguishable from "passed" in review evidence. Consolidated warning summaries
and selected NuGet diagnostic enforcement remain separate implementation work.

## Exceptions and baseline

Keep each accepted exception with its owning configuration or a linked durable record. Each record
must identify:

- Tool and exact rule/diagnostic identity.
- Exact file/project/path scope and current baseline quantity when countable.
- Rationale, evidence of impact/false positive, and why immediate remediation is unsuitable.
- Responsible maintaining team, remediation action, and expiry or explicit review date.
- Validation that proves the exception does not hide new findings outside its scope.

Prefer a rule-and-path exception over disabling a category globally. A budgeted baseline is a fixed
reviewed set or count, not permission for growth. New findings outside that baseline fail when
classified as blockers; additions to debt need a separate explicit review. Ratchet a baseline down
as findings are fixed. Do not regenerate a baseline to absorb new failures unnoticed.

For example, a generated-EF rule exception can name Infrastructure maintainers, the generated
migration glob, the generator limitation, and a review at the next SDK/EF upgrade plus a calendar
date. This is an example record shape, not a newly granted exception.

Existing suppressions with missing owner/date are legacy debt to inventory at the next touched-rule
review. This policy does not claim that all existing suppressions already meet the new record
requirements. Never convert confirmed secrets, known unsafe behavior, or missing security analysis
into ordinary maintainability debt.

## Review lifecycle

Review exceptions and baselines at least quarterly, at their recorded expiry, and whenever the
owning SDK, analyzer, package, scanner, or generator changes. The repository maintaining team owns
the cadence; feature owners own remediation in their scope. Remove obsolete exceptions with the
fix. An expired exception requires renewed evidence and review before it can justify further debt.

For a new diagnostic: identify the producer and exact rule, reproduce safely, classify impact and
confidence, then fix or record a narrowly reviewed exception. Preserve sanitized evidence and the
required check's exit status. Security findings go to security/repository maintainers immediately;
correctness and reliability blockers go to the owning application/suite maintainer before merge.

Validate policy changes against actual workflow commands and [trust boundaries](trust-boundaries.md).
Changing a required check or hosted gate also follows the [governance runbook](governance.md).

## External telemetry source identity exceptions

The application test maintainers own three method-scoped `SKTEST010` exceptions, reviewed on
2026-10-22, re-reviewed on 2026-10-25 for candidate `0.1.0-alpha.169.1` (which changes neither
`SKTEST010`, the Scheduling package nor the external instrumentation packages), and due for review by
2027-01-22 or the next owning-library/analyzer upgrade:

| File | Method | Reason |
| --- | --- | --- |
| `tests/ViajantesTurismo.Admin.ApiServiceTests/Documents/DocumentPrivacyTelemetryCapture.cs` | Constructor | ASP.NET Core and HTTP client instrumentation expose source names, without accessible singleton instances. |
| `tests/ViajantesTurismo.HostedTests/Catalog/Infrastructure/Tours/CatalogTourSlugLockPostgreSqlTests.cs` | `Catalog_npgsql_tracing_remains_enabled_without_first_response_or_parameter_values` | Npgsql owns the private source; its supported instrumentation selector is its name. |
| `tests/ViajantesTurismo.Catalog.UnitTests/PollingBackgroundServiceTestHarness.cs` | `CreateActivityListener` | Scheduling publishes its source name while keeping its singleton internal. |

The baseline is one attribute per named method, three total. Retain name filtering and trace-scoped
assertions so these privacy tests still observe the actual external instrumentation. Replace each
exception with instance identity if its owner exposes that instance. Application-owned sources use
instance identity. The existing suppression allowlist and full architecture tests reject unlisted
files; compiler enforcement remains enabled elsewhere. These exceptions are included for explicit
review with the candidate adoption change.
