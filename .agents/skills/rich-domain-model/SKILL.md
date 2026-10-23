---
name: rich-domain-model
description: Audit and refactor ViajantesTurismo domain code toward a rich, encapsulated, persistence-ignorant model. Use for anemic entities, non-private setters, exposed mutable collections, child entities changed outside their aggregate root, handlers, endpoints, or Razor pages deciding from raw domain state, enum branching outside the domain, SKDOM001 or SKDOM002 diagnostics, DDD convention test failures, and refreshing the domain model audit. Not for glossary or ubiquitous-language work, or EF mapping-only changes.
license: See repository LICENSE.txt
---

# Rich Domain Model Workflow

Use this skill to find and fix places where ViajantesTurismo domain rules leak out of the domain model. The rules
themselves live in `docs/domain/RICH_DOMAIN_MODEL.md`; this skill is the workflow.

## Read first

1. `docs/domain/RICH_DOMAIN_MODEL.md`: purity, encapsulation, behavior placement, smells, and the review checklist.
2. `docs/adr/20260703-rich-domain-behavior-and-state-exposure.md` (ADR-031), including its "Defer" list.
3. `docs/DDD_ANALYZER_RULE_CATALOG.md`: what the analyzers and architecture tests already enforce.
4. `docs/domain/domain-model-audit.md`: known findings and their status.
5. The nearest `AGENTS.md` for every file you touch.

## Workflow

1. **Scope**: pick a bounded context (`Admin`, `Catalog`) and, if possible, one aggregate.
2. **Audit** from the repository root. Each command lists candidates; review every hit by hand.
   - Domain enum branching outside the domain:

     ```bash
     enums=$(rg -o --no-filename -r '$1' 'public enum (\w+)' src/ViajantesTurismo.*.Domain | sort -u | paste -sd'|')
     rg -n --glob '!src/*.Domain/**' --glob '!**/bin/**' --glob '!**/obj/**' \
       -e "(==|!=|is( not)?|case)\s*(\w+\.)?($enums)\.\w+" src
     ```

   - One object's members combined in a decision outside the domain:

     ```bash
     rg -nP --glob '*.{cs,razor}' --glob '!**/bin/**' --glob '!**/obj/**' \
       '(?:if|&&|\|\|)\s*\(?!?\s*(\w+)\??\.\w+[^;]*(?:&&|\|\|)\s*!?\s*\1\??\.\w+' \
       src/ViajantesTurismo.*.Application src/ViajantesTurismo.*.ApiService src/ViajantesTurismo.*.Web
     ```

   - Unchecked `Result` values in the application layer: `rg -n '\)\.Value;' src/ViajantesTurismo.*.Application`
   - Exceptions and hard-coded bounds in the domain:
     `rg -n -e 'throw new' -e 'is < \d+ or > \d+' src/ViajantesTurismo.*.Domain`
   - Encapsulation is build-enforced: `SKDOM001`, `SKDOM002`, CA1002, CA1819, and CA2227 fail the domain build, and
     `DddConventionsTests` and `LayerDependencyTests` cover the child-entity boundary and purity.
3. **Classify** each hit with the behavior placement table in the guide: calculated property, domain method, value object,
   enum, explicit transition, application-level invariant (needs persistence or another aggregate), or no change (display,
   mapping, query projection, seed data, input guard).
4. **Prove** each actionable production gap with a failing test before changing production code: a unit test in
   `tests/ViajantesTurismo.<Context>.UnitTests`, a behavior specification, or an architecture test.
5. **Change** the smallest thing: move the rule into the aggregate or value object, update callers to use it, and delete
   duplicated checks. Keep EF mapping changes in Infrastructure and keep display facts public.
6. **Verify** with the commands in the guide's refactoring workflow, including the architecture tests:
   `dotnet test --project tests/ViajantesTurismo.ArchitectureTests/ViajantesTurismo.ArchitectureTests.csproj`.
7. **Record** each finding's status in `docs/domain/domain-model-audit.md`.

## Guardrails

- Do not add strategy objects, specifications, factories, or state hierarchies for a single simple switch.
- Do not hide display, mapping, or filtering state that callers legitimately need.
- Do not suppress `SKDOM001` or `SKDOM002`; follow the catalog's suppression policy if an exception is truly needed.
- Cite roadmap IDs such as `RM-149` in documentation, never issue or pull request numbers.
