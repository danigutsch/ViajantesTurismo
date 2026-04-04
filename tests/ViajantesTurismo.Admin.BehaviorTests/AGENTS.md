# AGENTS.md

Instructions for files under `tests/ViajantesTurismo.Admin.BehaviorTests/`.

## Scope and precedence

- Applies to all files under `tests/ViajantesTurismo.Admin.BehaviorTests/`.
- If instructions conflict with `tests/AGENTS.md`, follow this file for BDD/Reqnroll work.

## File placement

- Feature files belong under `specs/<Aggregate>/`.
- Step definitions belong under `StepDefinitions/`.

## Feature structure

- Include business-focused `Feature` and `Rule` blocks.
- Keep scenarios concise and outcome-oriented.
- Use `Background` only for truly shared setup.
- Avoid assertions in `Background`.

## Tagging

Use consistent tags, including:

- `@BC:<name>` for bounded context
- `@Agg:<name>` for aggregate
- `@regression` for suite inclusion
- Additional scenario tags like `@happy_path` and `@Entity:<name>` when relevant

## Step definition conventions

- Keep binding classes focused and deterministic.
- Prefer dependency injection for services/context.
- Keep step text at business intent level; move mechanics into helpers.
- Do not rely on static mutable state across scenarios.

## Scenario quality

- One behavior per scenario.
- Avoid implementation details in scenario names and step text.
- Use scenario outlines only when the same behavior repeats across clear data sets.

## References

- `tests/AGENTS.md`
- `tests/BDD_GUIDE.md`
- `docs/TEST_GUIDELINES.md`
