## Summary

- Harden contributor merge behavior for generated lockfiles so routine branch syncs do not produce noisy manual conflicts.
- Reintroduce a small, current-fit subset of the preserved contributor-workflow hardening tracked in `#178`.

## Changes

- Update `.gitattributes` to prefer the built-in `merge=binary` driver for generated workflow lock files.
- Add the same merge strategy for `packages.lock.json` files across the repository.

## Validation

- [ ] `dotnet build ViajantesTurismo.slnx`
- [ ] `dotnet test --solution ViajantesTurismo.slnx`
- [ ] CI lint (`bash scripts/lint-all.sh`)
- [x] Not run, with reason:
    - `dotnet build ViajantesTurismo.slnx`: not run; this change only adjusts git merge attributes for generated lockfiles.
    - `dotnet test --solution ViajantesTurismo.slnx`: not run; no runtime or test behavior changed.
    - `bash scripts/lint-all.sh`: not run manually; commit hook markdown lint passed during commit.

## Checklist

- [x] Commit messages follow Conventional Commits
- [x] Documentation updated where behaviour or workflow changed
- [ ] Screenshots or recordings attached for UI changes, if applicable
- [x] Related issue, backlog item, or ADR linked, if applicable
