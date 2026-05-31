# AGENTS.md

Instructions for files under `tests/ViajantesTurismo.ArchitectureTests/`.

## Scope and precedence

- Applies to all files under `tests/ViajantesTurismo.ArchitectureTests/`.
- If instructions conflict with `tests/AGENTS.md`, follow this file for architecture-test work.

## Architecture test rules

- Keep rules narrow, deterministic, and fast enough for routine local and CI execution.
- Prefer guarding stable repository conventions over speculative future architecture.
- When protecting Admin test architecture, treat `tests/README.md` as the canonical quick-reference and `docs/TEST_GUIDELINES.md` as the deeper rule set.
- Prefer assembly-level test metadata for project-wide tags unless a narrower class-level tag is genuinely needed.
- Avoid broad regex or reflection rules that would create noisy false positives across unrelated test projects.

## References

- `tests/AGENTS.md`
- `tests/README.md`
- `docs/TEST_GUIDELINES.md`
