# AGENTS.md

Instructions for files under `src/ViajantesTurismo.Admin.Web/`.

## Scope and precedence

- Applies to all files under `src/ViajantesTurismo.Admin.Web/`.
- If instructions conflict with higher-level `AGENTS.md` files, follow this file for Web-layer work.

## Project constraints

- This project is Blazor Server and intentionally not AOT-compatible.
- Do not add AOT publishing or compatibility settings here.

## Component structure

- Use `@page` only for page components.
- Keep layout components in `Components/Layout/`.
- Keep shared reusable components in `Components/Shared/`.
- Keep page components under `Components/Pages/`.
- For new or significantly refactored C# code in this project, keep one top-level type per file.

## Data access and API clients

- Use typed API clients (`I*ApiClient`) for all HTTP interactions.
- Do not call raw `HttpClient` directly in component code.

## UI state and errors

- Use explicit loading and error state fields.
- Reset error state before each new request.
- Display user-focused errors rather than raw exception output.

## Razor component behavior

- Keep substantial logic in `@code` methods, not inline markup expressions.
- Prefer deterministic rendering and explicit state transitions for testability.
- Preserve semantic markup and basic accessibility (labels, keyboard support).

## Testability

- Prefer patterns that remain easy to validate with bUnit.
- Avoid JS interop unless required.

## References

- `AGENTS.md` (repository root)
- `docs/CODING_GUIDELINES.md`
- `docs/TEST_GUIDELINES.md`
