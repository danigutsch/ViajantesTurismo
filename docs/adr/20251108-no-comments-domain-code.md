# ADR-005: No Comments in Domain Code

**Status**: Accepted — 2025-11-08

## Context

Inline comments in domain code often become outdated, add noise, and duplicate information already
expressed in well-named methods and validation errors. Maintaining comment accuracy is an overhead
that does not scale.

## Decision

**Remove inline comments** from domain entities. Document intent through:

- **XML comments** on public API surfaces (classes, public methods).
- **Separate markdown files** in `docs/` for complex patterns and architecture decisions.
- **Error messages** themselves that express business rules clearly.
- **Well-named methods and variables** that are self-documenting.

## Consequences

### Pros

- Code is more readable without comment clutter.
- Forces better naming conventions and expressive code.
- No risk of outdated comments contradicting code.
- Documentation in `docs/` folder is versioned and reviewed explicitly.

### Cons

- Less immediate context for complex business rules when reading code.
- May need to read error classes or docs to understand validation rules.

## Alternatives considered

- Keep inline comments for complex logic — rejected because comments drift over time.
- Require comments for all public methods — rejected in favor of XML doc comments for public API only.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- See `docs/CODING_GUIDELINES.md` for naming conventions.
