---
name: source-generation
description: Build, design, or fix C# Roslyn source generators; create incremental pipelines, add diagnostics, debug output, and add tests.
license: See repository LICENSE.txt
---

# Source Generation Workflow

Use this skill for C# Roslyn source generator work that needs structured design, implementation, and verification.

## When to use this skill

- Create a source generator.
- Migrate a generator to incremental.
- Add diagnostics to a generator.
- Fix generator output behavior.
- Add tests for generated code.

## Workflow

1. Define contract: triggers, eligible symbols, output, diagnostics.
2. Design pipeline: syntax filter, semantic projection, normalization, emission.
3. Implement deterministic transforms and emission.
4. Add actionable diagnostics with stable IDs.
5. Add tests for positive/negative/edge cases.
6. Verify build and tests.
