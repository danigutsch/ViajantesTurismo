---
description: 'Standards for designing and implementing C# Roslyn source generators with deterministic output, diagnostics, and testability.'
applyTo: '**/*.{cs,csproj}'
---

# C# Source Generation Standards

Use these rules when creating, modifying, or reviewing C# Roslyn source generators.

## Design rules

- Define a clear generator contract before coding: trigger, eligible symbols, emitted artifacts, and diagnostics.
- Prefer incremental architecture with stable pipeline stages.
- Separate discovery, projection, emission, and diagnostics.

## Determinism and safety

- Do not emit timestamps, random values, or machine-specific paths in generated code.
- Do not swallow failures; report actionable diagnostics.
- Emit only for symbols that match the explicit contract.

## Testing expectations

- Add tests for expected generated output and diagnostics.
- Add edge tests for nested types, generics, and nullability.
- Keep output stable between runs.
