# Analyzer Hardening Roadmap

This document records the staged analyzer-adoption plan for issue `#132`.

It covers:

- existing analyzer adoption first
- current SharedKernel analyzer/code-fix status
- remaining implementation backlog
- migration guidance for raising enforcement safely

## Goal

Move repository convention enforcement out of docs-plus-review where practical and into a
maintained analyzer stack with staged rollout.

## Current baseline

The repository already has a stronger analyzer baseline than when `#132` was opened.

### Build-level baseline

- `Directory.Build.props` enables `TreatWarningsAsErrors`, `CodeAnalysisTreatWarningsAsErrors`,
  `EnforceCodeStyleInBuild`, `AnalysisLevel=latest`, and `AnalysisMode=All`.
- `SonarAnalyzer.CSharp` is referenced centrally for ordinary builds.
- `.editorconfig` owns most scoped severity tuning and exception policy, while test-project-wide
  exceptions still include a small `NoWarn` set in `tests/Directory.Build.props`.

### SharedKernel analyzer families already shipped

- `SharedKernel.Style.Analyzers`
    - `SKSTYLE001` no `Async` suffix
    - `SKSTYLE002` `CancellationToken` parameters must be named `ct`
    - `SKSTYLE003` `CancellationToken` parameters must not declare default values
    - `SKSTYLE004` one top-level type per file, staged as a rollout rule
- `SharedKernel.Style.CodeFixes`
    - safe rename for `SKSTYLE001`
    - safe rename for `SKSTYLE002`
- `SharedKernel.Testing.Analyzers`
    - `SKTEST001` forbid local pragma suppression inside xUnit test methods
    - `SKTEST002` enforce underscore naming for xUnit test methods
- `SharedKernel.Testing.CodeFixes`
    - conservative rename for `SKTEST002`
- `SharedKernel.Mediator.Analyzers`
    - implemented mediator handler, pipeline, and cancellation diagnostics for the current
      mediator contract surface
- `SharedKernel.Mediator.SourceGenerator`
    - remains the generator-owned source of mediator discovery diagnostics and generated dispatch
      behavior

### Related issues already closed or split out

- `#125` established the SharedKernel style analyzer/code-fix family
- `#145` landed `SKTEST001`
- `#163` landed the `CancellationToken` convention rules

That means `#132` is now primarily a roadmap, adoption, and coordination item rather than a
greenfield analyzer implementation issue.

## Canonical severity policy

`.editorconfig` is the canonical severity matrix for C#, style, Sonar, and SharedKernel analyzer
policy. Project files and `Directory.Build.props` can temporarily carry approved `NoWarn` entries
only when the exception is tracked by this roadmap and guarded by architecture tests.

Severity ownership rules:

- `.editorconfig` owns ordinary diagnostic severity, generated-code exceptions, and file/path-scoped
  rollout exceptions.
- `Directory.Build.props` owns analyzer package references and build enforcement switches, not
  diagnostic-specific suppressions.
- Project files should avoid `NoWarn`; any remaining entry must have an issue owner, removal path,
  and suppression-regression allowlist entry.
- Generated files, migrations, code-generation fixtures, and analyzer test fixtures should be
  separated from hand-written source policy.

Approved broad suppression owners:

The architecture test uses this allowlist as the approved broad-suppression inventory. Entries may
represent current suppressions or staged removals that remain approved during branch transitions.

| Surface | Diagnostics | Owner | Removal path |
| --- | --- | --- | --- |
| `tests/Directory.Build.props` | `CA2007`, `CA5394`, `CS1591`, `S107` | Test maintainers | Move into source-scoped `.editorconfig` policy or fix test patterns |
| `src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj` | `CS1591` | API maintainers | Document public framework entrypoints or move to scoped `.editorconfig` |
| `src/ViajantesTurismo.Management.Web/ViajantesTurismo.Management.Web.csproj` | `CS1591` | Management Web maintainers | Document public component/model surfaces or move to scoped `.editorconfig` |
| `tests/ViajantesTurismo.Admin.BehaviorTests/ViajantesTurismo.Admin.BehaviorTests.csproj` | `CA1812` | Behavior-test maintainers | Move to scoped `.editorconfig` for Reqnroll-discovered binding types |

The architecture-test suppression guard owns this allowlist and should fail if new broad
suppression surfaces appear without review.

## Adoption matrix

The matrix below focuses on the high-value rules and families that matter to repository policy.

| Area | Rule or family | Source | Current state | Current severity | Exception policy | Next step |
| --- | --- | --- | --- | --- | --- | --- |
| Public guard clauses | `CA1062` validate arguments of public methods | Built-in .NET analyzers | Adopted | `error` in production; scoped exceptions only in migrations and Admin BDD step-definition files | Reusable test helpers follow the same guard-clause expectations as production support code; framework-owned step bindings stay on a narrow file-scope exception path | Keep the exception list narrow and avoid reintroducing project-wide suppression |
| Async naming | `SKSTYLE001` no `Async` suffix | `SharedKernel.Style.Analyzers` | Adopted | `suggestion` | Overrides and interface implementations allowed through config | Raise after remaining cleanup is low-risk |
| CancellationToken name | `SKSTYLE002` require `ct` | `SharedKernel.Style.Analyzers` | Adopted | `suggestion` | Keep narrow scoped exceptions only when external contracts force a different name | Raise after repo cleanup |
| CancellationToken defaults | `SKSTYLE003` forbid `CancellationToken ct = default` | `SharedKernel.Style.Analyzers` | Adopted | `suggestion` | Same as above | Raise after repo cleanup |
| One top-level type per file | `SKSTYLE004` | `SharedKernel.Style.Analyzers` | Adopted with staged exclusions | `suggestion` globally, `none` in tests and explicit file exceptions | Small explicit allowlist in `.editorconfig` | Reduce allowlist over time |
| Test pragma suppressions | `SKTEST001` | `SharedKernel.Testing.Analyzers` | Adopted | package default `warning` | Test-only by design | Keep active and narrow |
| Test naming | `SKTEST002` | `SharedKernel.Testing.Analyzers` | Adopted | package default `warning` | Test-only by design | Keep active and use code fix during cleanup |
| Mediator cancellation forwarding | `SKMED006` | `SharedKernel.Mediator.Analyzers` | Adopted | repository-configured `warning` | `.editorconfig`-tunable | Keep active |
| Stream cancellation semantics | `SKMED007`, `SKMED008` | `SharedKernel.Mediator.Analyzers` | Adopted | `warning` / `info` | `.editorconfig`-tunable | Keep active |
| CQRS strict handler-to-handler send | `SKMED500` | `SharedKernel.Mediator.Analyzers` | Adopted | `suggestion` | disabled by config if needed | Keep staged |
| General built-in code style | selected `IDE*` rules such as `IDE0005`, `IDE0028` | Built-in Roslyn style analyzers | Adopted selectively | mixed `warning`/`suggestion` | `.editorconfig`-owned | Keep tuning by signal |
| Sonar repository rules | selected `S*` rules | `SonarAnalyzer.CSharp` | Adopted selectively | mixed | narrow file-scope suppressions in tests/steps | Keep scoped and documented |

## Fixer-first rollout workflow

Every analyzer hardening issue should follow the same remediation order:

1. Inventory current diagnostics and suppression surfaces.
2. Run `dotnet format analyzers` for the target diagnostic when the rule is fixable by the SDK or
   installed analyzer packages.
3. Prefer existing Roslyn, IDE, Sonar, or package-provided code fixes before manual edits.
4. If violations are recurring and mechanical but no safe existing fixer exists, create or extend a
   SharedKernel code-fix package before raising severity.
5. If the rule is generated-code-only or a one-off exception, document the exception instead of
   creating a custom fixer.
6. Avoid broad suppression as the default remediation. Use file/path-scoped `.editorconfig` entries
   with owner and removal conditions when exceptions are justified.

Issue updates and PR descriptions should record which fixer path was tried. A rule may move to
`warning` or `error` only after its existing baseline is either fixed or explicitly scoped.

## Suppression regression guard

`ViajantesTurismo.ArchitectureTests` includes a repository-level analyzer suppression policy guard.
It blocks unreviewed additions of:

- project or props `NoWarn` entries
- local `#pragma warning` suppression in hand-written source
- `SuppressMessage` attributes outside approved exception surfaces

Allowed suppression surfaces must remain explicit in the architecture test. Additions require an
issue owner, a removal condition, and a narrow scope. Generated EF migrations, analyzer test
fixtures, and the current mediator sample assembly suppression are the only approved pragma or
attribute surfaces at this stage. The guard excludes build outputs, package restore folders, local
worktrees, and Reqnroll-generated `.feature.cs` files so the policy is applied to maintained
repository source instead of generated or external code.

## Code-fix backlog

Custom code fixes should be reserved for recurring repository patterns where the remediation is
local, deterministic, and safe.

| Candidate | Owning package | Priority | Rationale | Status |
| --- | --- | --- | --- | --- |
| `SKSTYLE004` split one top-level type per file | `SharedKernel.Style.CodeFixes` | Medium | Repeated staged rollout cleanup; can offer safe file extraction only when names and paths are obvious | Candidate |
| `CA1062` guard insertion for repository public APIs | None yet | Low | Built-in IDE fixes exist in many cases; custom fixer risks low-value boilerplate | Defer unless recurring non-fixable cases remain |
| `CS1591` XML documentation generation | None | Low | Existing IDE support is adequate; generated comments are often low-signal | Defer |
| `S107` constructor/test-helper parameter reduction | None | Low | Requires design judgment, not mechanical code fix | Do not automate |
| `CA2007` ConfigureAwait insertion | None | Low | SDK/IDE fixes generally exist; app-layer policy differs from reusable-library policy | Use existing fixes only |
| `CA5394` deterministic randomness in tests | `SharedKernel.Testing.CodeFixes` | Low | Test cases usually need intent-specific random/data-builder decisions | Defer unless repeated safe pattern emerges |

Accepted custom fixer work must include analyzer/code-fix tests and README/`AnalyzerReleases.*`
updates in the owning SharedKernel package.

## Phase plan

### Phase 1: maximize existing analyzers first

Status: largely in progress already.

Objectives:

1. Keep built-in and centrally available analyzers as the first enforcement layer.
2. Use `.editorconfig` for severity tuning and scoped exceptions instead of broad project-level
   suppression.
3. Keep a small, explicit allowlist for staged rollout rules.
4. Document rule ownership and current posture clearly so future cleanup is deliberate.

Concrete Phase 1 backlog:

1. Review existing `IDE*`, `CA*`, and `S*` severities for low-noise rules that can move from
   `suggestion` to `warning`.
2. Shrink file-specific `SKSTYLE004` exceptions as grouped top-level-type files are refactored.
3. Measure remaining repository violations before raising `SKSTYLE001`, `SKSTYLE002`, or
   `SKSTYLE003` above `suggestion`.
4. Keep `CA1062` out of `tests/Directory.Build.props`; use real guards in reusable test support
   code and only narrow file-scoped exceptions where framework-owned binding entrypoints would
   otherwise force low-value boilerplate.
5. Keep all exceptions in `.editorconfig` file-scoped where possible; avoid `NoWarn` expansion as
   the default answer.

### Phase 2: extend SharedKernel custom analyzers and code fixes

Status: partially complete.

Already landed from this phase:

- repo-wide style analyzer family
- test-only analyzer family
- test-only pragma suppression rule
- `CancellationToken` naming/default-value rules

Remaining Phase 2 backlog:

1. Add further repository-specific style rules only where built-in analyzers do not cover the
   convention well enough.
2. Prefer code fixes for high-frequency rename or signature-shape rules.
3. Keep test-only rules in `SharedKernel.Testing.*`, not in repository-wide production analyzer
   packages.
4. Keep mediator-specific rules in `SharedKernel.Mediator.*` and avoid duplicating generator-owned
   diagnostics in style analyzers.

Candidate future custom rules still worth evaluating:

- error classes must end with `Errors`
- domain events must end with `DomainEvent`
- integration events must end with `IntegrationEvent`
- focused guardrails for known repository architecture conventions that are too specific for
  off-the-shelf analyzers and too noisy for architecture tests alone

## Source-generator role

Source generation should remain selective.

Guidance:

- use analyzers when the need is detection, staged enforcement, or code-fix-assisted cleanup
- use code fixes when the remediation is local and safe
- use source generation only when generated code materially improves ergonomics or eliminates
  repetitive boilerplate

For this repository, source generation is already justified in the mediator stack. It should not be
the default answer for general style enforcement.

## Compatibility plan with existing SharedKernel tooling

Keep the current package boundaries stable:

- `SharedKernel.Style.Analyzers` and `SharedKernel.Style.CodeFixes`
    - repository-wide production/style conventions
- `SharedKernel.Testing.Analyzers` and `SharedKernel.Testing.CodeFixes`
    - test-only conventions
- `SharedKernel.Mediator.Analyzers`, `SharedKernel.Mediator.CodeFixes`, and
  `SharedKernel.Mediator.SourceGenerator`
    - mediator contract, dispatch, and generator diagnostics

Compatibility rules:

1. Do not move test-only diagnostics into production analyzer packages.
2. Do not duplicate generator-owned mediator diagnostics in style analyzers.
3. Keep `AnalyzerReleases.*.md`, README diagnostics tables, and tests aligned per analyzer family.
4. Reuse the existing Roslyn packaging and test harness patterns already established in the
   SharedKernel analyzer and code-fix projects.

## Migration guidance for teams

When adopting or tightening analyzer rules:

1. Start with inventory.
   Identify whether the rule is already covered by built-in analyzers, Sonar, SharedKernel style,
   SharedKernel testing, mediator analyzers, or architecture tests.
2. Prefer the smallest enforcement mechanism.
   Do not build a new custom analyzer if a built-in rule plus scoped configuration already solves
   the problem.
3. Stage severities.
   Use `suggestion` first for cleanup-heavy rules, then raise to `warning` or `error` after the
   repository baseline is under control.
4. Keep exceptions narrow.
   Prefer file-scoped `.editorconfig` exceptions over project-wide `NoWarn` or broad analyzer
   suppression.
5. Pair rollout with fixes.
   If the rule is high-frequency and mechanical, provide a code fix before raising severity.
6. Keep docs and release notes current.
   Update analyzer README files, `AnalyzerReleases.*`, and repo guidance when adding or promoting a
   rule.

## Recommended near-term backlog order

1. Audit current `SKSTYLE004` exception files and retire the ones that no longer need grouped
   top-level types.
2. Measure repository violations for `SKSTYLE001` through `SKSTYLE003` and decide whether any can
   move from `suggestion` to `warning`.
3. Remove the broad test-project `CA1062` `NoWarn` suppression and replace it with narrower
   exceptions only where test code still has a justified boundary reason.
4. Review existing `IDE*` and `CA*` severities for additional high-signal candidates that can be
   promoted without broad churn.
5. Evaluate the next repo-specific custom style rules only after that built-in and already-shipped
   analyzer adoption work is complete.

## Recommendation summary

- Treat `#132` as the umbrella roadmap and adoption record.
- Treat closed implementation issues such as `#125`, `#145`, and `#163` as completed slices under
  that roadmap.
- Prefer adopting and tuning existing analyzers before inventing more custom rules.
- Keep custom analyzers focused on conventions the built-in and Sonar layers do not enforce well.

## References

- `.editorconfig`
- `Directory.Build.props`
- `docs/CODING_GUIDELINES.md`
- `docs/CODE_QUALITY.md`
- `src/SharedKernel/SharedKernel.Style.Analyzers/README.md`
- `src/SharedKernel/SharedKernel.Style.CodeFixes/README.md`
- `src/SharedKernel/SharedKernel.Testing.Analyzers/README.md`
- `src/SharedKernel/SharedKernel.Testing.CodeFixes/README.md`
- `src/SharedKernel/SharedKernel.Mediator.Analyzers/README.md`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/README.md`
