---
description: "Use when creating, editing, or refactoring domain entities, aggregates, value objects, domain errors, and domain rules in ViajantesTurismo. Enforces Result pattern, factory methods, aggregate boundaries, and Domain layer purity."
name: "ViajantesTurismo Domain Guidelines"
applyTo: "src/ViajantesTurismo.Admin.Domain/**/*.cs"
---

# Domain Implementation Guidelines

- Keep Domain layer pure: no API, persistence, or infrastructure concerns.
- Follow `docs/DOMAIN_VALIDATION.md` and `docs/CODING_GUIDELINES.md`.

## Domain Validation and Construction

- Prefer static factory methods (`Create`) returning `Result<T>`.
- Keep constructors private to prevent invalid instances.
- Use update methods returning `Result` for state changes.
- Use `ContractConstants` from `src/ViajantesTurismo.Admin.Contracts/ContractConstants.cs` for shared limits.

## Error Handling

- Use `Result` / `Result<T>` for expected validation failures.
- Avoid exceptions for business validation; reserve exceptions for truly exceptional/invariant-break situations.
- Use dedicated `*Errors` classes per aggregate/value object family.

## Aggregate and Modeling Rules

- Enforce aggregate boundaries (for example, booking lifecycle changes flow through `Tour`).
- Keep business rules inside aggregates/value objects, not in application or API layers.
- Prefer explicit domain types for clarity and model intent.
- Keep value objects immutable and validation-focused.
- If guidance here conflicts with repository-wide instructions, prefer the domain-scoped rules for files under
`src/ViajantesTurismo.Admin.Domain/`.

## Style and Documentation

- Follow `docs/CODING_GUIDELINES.md` and `.editorconfig`.
- Use file-scoped namespaces and 4-space indentation.
- Keep public APIs documented with XML comments.
- Preserve naming conventions from `docs/CODING_GUIDELINES.md`.

## References

- `docs/DOMAIN_VALIDATION.md`
- `docs/CODING_GUIDELINES.md`
- `docs/ARCHITECTURE_DECISIONS.md`
- `docs/adr/`
- `src/ViajantesTurismo.Common/FUNCTIONAL_PATTERNS.md`
