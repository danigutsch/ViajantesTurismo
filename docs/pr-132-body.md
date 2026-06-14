## Summary

- Implement the first concrete test-only analyzer slice under `#132` by grouping two closely related xUnit enforcement rules into one PR.
- Add immediate editor/build diagnostics for local pragma-warning suppressions in xUnit tests
  and for xUnit test methods that do not follow the repository underscore naming convention.

## Changes

- Extend `SharedKernel.Testing.Analyzers` with `SKTEST002` for xUnit test method naming.
- Keep and document `SKTEST001` for local pragma warning suppression inside xUnit test methods.
- Add the first `SharedKernel.Testing.CodeFixes` implementation: a conservative rename fix for `SKTEST002` when the target underscore name is safe.
- Extend `tests/SharedKernel.Testing.Analyzers.Tests` to cover:
    - `SKTEST001` diagnostic behavior
    - `SKTEST002` diagnostic behavior
    - rename code-fix behavior and conflict refusal
- Update analyzer release/public API metadata and the testing analyzer/code-fix READMEs.

## Validation

- [ ] `dotnet build ViajantesTurismo.slnx`
- [ ] `dotnet test --solution ViajantesTurismo.slnx`
- [ ] CI lint (`bash scripts/lint-all.sh`)
- [x] Not run, with reason:
    - `dotnet build ViajantesTurismo.slnx`: not run separately; the dedicated testing analyzer test project compiled as part of validation.
    - `dotnet test --solution ViajantesTurismo.slnx`: not run; validation stayed scoped to the testing analyzer family for this focused slice.
    - `bash scripts/lint-all.sh`: not run manually; commit hook markdown lint passed during commit.
    - Focused validation: `dotnet test --project "tests/SharedKernel.Testing.Analyzers.Tests/SharedKernel.Testing.Analyzers.Tests.csproj"`

## Checklist

- [x] Commit messages follow Conventional Commits
- [x] Documentation updated where behaviour or workflow changed
- [ ] Screenshots or recordings attached for UI changes, if applicable
- [x] Related issue, backlog item, or ADR linked, if applicable
