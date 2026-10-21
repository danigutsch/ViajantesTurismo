# Coupling and cohesion measurement proposal

Status: research recommendation; no new collector, dependency, alert, or merge blocker is installed.
Owner: application architecture maintainers. Reviewed October 20, 2026.
Scope: ViajantesTurismo application projects. Referenced reusable packages are external boundary
nodes for this report, not targets for library changes.

## Contents

- [Recommended signals](#recommended-signals)
- [Reproducible pipeline](#reproducible-pipeline)
- [Interpretation and rollout](#interpretation-and-rollout)
- [Research](#research)

## Recommended signals

Start with the existing project-reference diagram and architecture tests. A prohibited dependency
is a concrete boundary violation; a large dependency count alone is not. Report semantic type
metrics only after the pinned compiler can produce stable results for this repository's C# syntax.

| Signal | Definition and data source | Interpretation and limitation |
| --- | --- | --- |
| Outgoing project coupling | Number of distinct direct ProjectReference targets per application project, from evaluated MSBuild references | Indicates dependency breadth; exclude duplicate references. Record target framework/configuration and conditional references; source XML diagrams alone are a navigation aid |
| Incoming project coupling | Number of distinct application projects directly referencing a project in the same evaluated graph | Identifies change blast radius; contracts naturally have high incoming coupling and are not candidates for deletion solely for that reason |
| Project cycles and forbidden edges | Strongly connected components with more than one node, and edges prohibited by existing architecture rules | Report independently of counts; enforce existing architecture tests rather than inventing a second policy engine |
| Type coupling | Distinct referenced types per hand-written type, as defined by a pinned semantic analyzer | Separate own-context, other-context, framework, and package references; compiler/tool definitions affect comparability |
| Method cohesion proxy | Fraction of method pairs sharing access to at least one instance field, among instance methods that access state; report numerator and denominator | Proposed custom measure, not a universal LCOM score. Exclude constructors/static/generated members; no eligible pair means unavailable, not zero cohesion |
| Contextual change evidence | Number of reviewed changes touching multiple capability folders over a fixed window, paired with concrete PR examples | Co-change can indicate coupling or one deliberate migration. Renames and mechanical formatting must be separated; never score developer performance |

Do not treat maintainability index, inheritance depth, cyclomatic complexity, or method count as
interchangeable measures of cohesion. Aggregate orchestration, immutable records, adapters, and
contracts have different responsibilities. Compare like roles and inspect behavior before proposing
splits. Moving a reference through a wrapper can lower a number while making the design worse.

## Reproducible pipeline

Proposed implementation sequence, using existing repository tooling boundaries:

1. Select application projects from solution membership and record the exact Git commit, base
   commit, SDK, tool versions, OS, target framework, build configuration, and exclusion list.
2. Evaluate MSBuild references after locked restore. Run semantic metrics on the same compilation
   inputs only if an existing or separately approved pinned tool supports them. Do not infer missing
   semantic facts with regex or silently switch tool versions.
3. Normalize project/type identities and sort output deterministically. Preserve rename mappings
   for comparisons. Mark missing/failed measurements explicitly; do not report zero.
4. Emit a proposed `TestResults/architecture-metrics/report.json` and a short Markdown summary as
   CI artifacts. Store baseline SHA and schema/tool version in the report. Keep source and generated
   code out of the artifact; the repository need not accumulate per-commit metric files.
5. Compare only compatible reports. A tool/schema/exclusion change creates a new baseline, retaining
   the old report and recording why historical values are not directly comparable.

Suggested report fields are `schemaVersion`, `headSha`, `baseSha`, `sdkVersion`, `toolVersions`,
`configuration`, `targetFramework`, `exclusions`, and records containing `subject`, `metric`,
`value`, `numerator`, `denominator`, and `status`. An exception additionally needs scope, rationale,
owner, review date, and linked evidence under the [warning policy](../ci/warning-policy.md).
This is a proposed format, not a new checked-in schema or command.

Keep artifacts for the existing CI retention window initially; promote a reviewed baseline artifact
only when starting trend comparison. If durable trend history becomes necessary, decide storage
and retention separately rather than adding an external service or a database now.

## Interpretation and rollout

Collect four representative weekly samples before proposing numeric thresholds. Include ordinary
feature work and a deliberate source move; review whether the tool reports meaningful differences.
Show absolute value, delta, role, and measurement availability. Limit a PR summary to five highest
review candidates, with a link to the full artifact; do not generate mass annotations.

After the observation window, use advisory reports for sustained rises or cross-context edges that
need review. Architecture maintainers review trends monthly and classify concrete findings. Prefer
a specific boundary test or behavior-preserving ownership improvement over a global coupling cap.
No metric-only failure blocks a merge under this proposal.

A new blocker requires a separate approved policy, reproducibility on supported hosts, an owned
baseline, an exemption lifecycle, evidence from real regressions, and a low false-positive rate.
Existing forbidden-reference tests remain blockers. Exemptions are narrow and expire/review on a
named date; they do not hide failures outside their subject. Never split a business invariant or
introduce a dependency merely to improve a metric.

## Research

The metric definitions and measurement cautions were reviewed against
[Microsoft's code metrics reference](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-values).
It documents class coupling, maintainability, complexity, and inheritance signals; this proposal's
project graph, cohesion proxy, and rollout are repository-specific choices. None is a measured
baseline yet. Existing [boundary diagrams](boundaries-and-dependencies.md),
[architecture tests](../../tests/ViajantesTurismo.ArchitectureTests/README.md), and
[vertical slice plan](../vertical-slice-migration-plan.md) supply current ownership context.
