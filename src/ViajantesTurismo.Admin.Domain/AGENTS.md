# AGENTS.md

Instructions for files under `src/ViajantesTurismo.Admin.Domain/`.

This file overrides root guidance where domain-specific behavior is needed.

## Scope and precedence

- Applies to all C# domain files in this folder tree.
- If instructions conflict with root `AGENTS.md`, follow this file for domain work.

## Domain modeling rules

- Keep the Domain layer pure: no API, infrastructure, or persistence concerns.
- Keep business rules inside aggregates and value objects.
- Enforce aggregate boundaries (for example, booking lifecycle flows through `Tour`).
- Use `ContractConstants` for shared validation limits.

## Validation and error handling

- Use factory methods and private constructors to keep entities valid from creation.
- Return `Result` / `Result<T>` for expected validation failures.
- Do not use exceptions for normal business validation outcomes.
- Use dedicated `*Errors` classes for aggregate/value-object error creation.

## Style and structure

- Follow `docs/CODING_GUIDELINES.md` and `.editorconfig`.
- Use file-scoped namespaces and 4-space indentation.
- Use explicit domain types where clarity matters.
- Keep public APIs documented with XML docs.

## References

- `docs/DOMAIN_VALIDATION.md`
- `docs/CODING_GUIDELINES.md`
- `docs/ARCHITECTURE_DECISIONS.md`
- `docs/adr/`
- `src/ViajantesTurismo.Common/FUNCTIONAL_PATTERNS.md`
