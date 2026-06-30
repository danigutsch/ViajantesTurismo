# Generated Dispatching Evaluation

This document records the generated dispatching evaluation, which reviewed older
dispatching experiments preserved on stale local branches and compared them against the
current `SharedKernel.Mediator` source-generator direction.

## Reviewed preserved commit set

The preserved branch history for this evaluation was:

- `c53505a0` `feat(dispatching): simplify request dispatching by unifying Send method signatures`
- `6250c995` `feat(dispatching): add generated dispatching support and update service registrations`
- `89204887` `refactor: remove mixed registration styles diagnostics and related code`
- `442028a4` `refactor: update dispatching registrations and improve analyzer diagnostics`
- `99c3eaff` `feat(tests): add project references for dispatching generators and Roslyn`

## Current repository baseline

The old branch worked in a previous `ViajantesTurismo.Common.Dispatching` model built around:

- runtime `IRequestDispatcher` resolution
- per-request `IRequestDispatchRoute<TResponse>` route discovery from DI
- explicit runtime route objects to stay AOT-safe
- registration patterns that mixed generator-assisted dispatching with runtime service lookup

The repository no longer uses that architecture as the main direction.

Current dispatching is centered on `SharedKernel.Mediator` and already provides:

- generated `AppMediator` request and notification dispatch
- generated DI registration through `AddSharedKernelMediator`
- generated pipeline invocation
- generated typed request dispatch plus internal object dispatch support
- analyzer coverage for handler, stream, pipeline, and cancellation rules
- package-consumption and generator-heavy tests in the `Mediator Heavy Tests` CI lane

Relevant current sources:

- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/AppMediatorEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/GeneratedDispatchEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/DependencyInjectionEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.Analyzers/SharedKernelMediatorAnalyzer.cs`

## Evaluation outcome

### 1. Unified `Send` signatures

#### Preserved idea for unified `Send`

The old branch simplified request dispatching by preferring a single request-shaped `Send`
entry point instead of carrying both request-specific and extra generic route forms.

#### Current status for unified `Send`

This direction is already effectively adopted.

The public sender contract today is:

- `ValueTask<TResponse> ISender.Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)`
- `IAsyncEnumerable<TResponse> ISender.Send<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct)`

The generator still emits strongly typed overloads on `AppMediator`, but those are an
implementation detail of the generated mediator shell rather than a separate public routing
abstraction.

#### Decision for unified `Send`

Do not revive old `IRequestDispatcher` or route abstractions for this goal.

Status: already superseded by the current mediator API shape.

### 2. Generated dispatching support

#### Preserved idea for generated dispatching support

The old branch introduced generator-backed dispatching and generator-aware service
registration.

#### Current status for generated dispatching support

This idea is not merely reusable; it is already the repository baseline.

Today the source generator emits:

- generated request dispatch switches
- generated stream dispatch switches
- generated notification publication logic
- generated DI registrations for handlers and pipelines
- generated mediator instrumentation hooks

The old branch's runtime route objects would now duplicate the generated mediator shell
instead of extending it.

#### Decision for generated dispatching support

Do not revive the old runtime route model.

Status: already implemented in a stronger form by `SharedKernel.Mediator`.

### 3. Registration updates and mixed-style diagnostic removal

#### Preserved idea for registration cleanup

The old branch cleaned up registration shape and removed diagnostics tied to mixed manual vs.
generated registration styles.

#### Current status for registration cleanup

This also aligns with the present repository direction.

The current generator owns the primary mediator registration path through
`AddSharedKernelMediator`, and analyzer effort has moved toward implemented handler/pipeline
correctness rules rather than protecting a hybrid runtime-registration model.

#### Decision for registration cleanup

Do not resurrect mixed-style registration diagnostics or the older dispatching registration
surface.

Status: obsolete under the current generator-owned registration model.

### 4. Test-project references for generator and Roslyn coverage

#### Preserved idea for test coverage

The old branch added explicit test coverage for generator-related project references.

#### Current status for test coverage

This remains directionally valid, but the repository already has a better replacement:

- generator tests
- package-consumption tests
- code-fix tests
- CI documentation that treats `Mediator Heavy Tests` as the generated-output guardrail lane

#### Decision for test coverage

Keep the current `Mediator Heavy Tests` safety model.

Status: useful intent already preserved by newer test infrastructure.

## What should stay dead

These parts of the old experiment should stay retired:

- `ViajantesTurismo.Common.Dispatching` runtime route abstractions such as
  `IRequestDispatchRoute<TResponse>`
- `RequestDispatcher` as the main dispatch runtime
- hybrid dispatching where generated support feeds a runtime route lookup layer
- analyzer work aimed at protecting transitional mixed registration styles

They solve a transition-state architecture the repository has already moved past.

## Small backlog candidates still worth keeping in mind

This evaluation does not justify reviving the old branch, but it does suggest a few narrow
follow-up questions for the current mediator stack:

1. Keep public mediator contracts request-shaped and avoid reintroducing extra public generic
   dispatch forms unless a concrete consumer proves they are necessary.
2. Keep generator, package-consumption, and analyzer tests aligned whenever mediator public
   contracts change.
3. If future work needs object-based dispatch externally, prefer extending the current
   `SharedKernel.Mediator` contract deliberately instead of reviving route-based dispatching
   infrastructure.

## Recommendation summary

- **Revive old branch code directly:** No
- **Adopt old dispatching abstractions into current source-generator work:** No
- **Treat preserved branch as historical input only:** Yes
- **Carry forward any ideas:** Only the already-absorbed direction toward generator-owned,
  request-shaped dispatch and explicit validation coverage

## Final decision

This evaluation should be treated as a decision record, not as a code-porting task.

The preserved branch contained useful transitional thinking, but the repository has already
landed on a cleaner destination:

- generated mediator shell instead of runtime route objects
- generator-owned DI registration instead of mixed dispatching registration styles
- analyzer and package-consumption guardrails instead of transition-specific diagnostics

No source code from the retired dispatching branch should be revived as-is.

## References

- `src/SharedKernel/SharedKernel.Mediator.Abstractions/ISender.cs`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/AppMediatorEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/GeneratedDispatchEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/DependencyInjectionEmitter.cs`
- `src/SharedKernel/SharedKernel.Mediator.Analyzers/SharedKernelMediatorAnalyzer.cs`
- `docs/ci/main-workflow.md`
- `docs/ci/telemetry-generated-guardrails.md`
