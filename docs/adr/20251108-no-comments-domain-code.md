# ADR-005: No Comments in Domain Code

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Comments in domain code often become outdated and add noise. Well-named methods and validation errors are
self-documenting.

## Decision

Remove inline comments from domain entities. Document:

- Public API with XML comments on classes/public methods
- Complex patterns in separate markdown files
- Business rules in error messages themselves

## Consequences

### Positive

- Code is more readable
- Forces better naming
- No outdated comment maintenance
- Documentation in docs/ folder stays current

### Negative

- Less context for complex business rules
- May need to read error classes for validation rules
