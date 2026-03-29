# AGENTS.md

Repository-wide agent instructions for ViajantesTurismo.

This file is intended to stay concise and cover only repository-wide guidance.
Use nested `AGENTS.md` files and `.github/instructions/*.instructions.md` for scoped rules.

## Scope and precedence

- Applies to the entire repository.
- More specific `AGENTS.md` files and `.github/instructions/*.instructions.md` files override this file for their scope.
- Current scoped guidance includes:
    - `src/ViajantesTurismo.Admin.Domain/AGENTS.md`
    - `tests/AGENTS.md`
    - `.github/instructions/backend-domain.instructions.md`
    - `.github/instructions/tests.instructions.md`
    - `.github/instructions/integration-tests.instructions.md`

## Repository-wide coding rules

- Follow `docs/CODING_GUIDELINES.md` and `.editorconfig`.
- Use file-scoped namespaces, 4-space indentation, nullable reference types, and XML docs on public APIs.
- Do not append the `Async` suffix to method names.
- Treat warnings as errors (`Directory.Build.props`).

## Architecture

- Respect Clean Architecture boundaries:
    - Domain: `src/ViajantesTurismo.Admin.Domain`
    - Application: `src/ViajantesTurismo.Admin.Application`
    - Infrastructure: `src/ViajantesTurismo.Admin.Infrastructure`
    - API: `src/ViajantesTurismo.Admin.ApiService`
- Keep business rules in the Domain layer; do not move them into API or infrastructure code.

## Build and validation

- Setup: `setup-dev.ps1` (Windows) or `setup-dev.sh` (Unix).
- Build: `dotnet build ViajantesTurismo.slnx`.
- Run app: `dotnet run --project src/ViajantesTurismo.AppHost`.
- Run tests: `dotnet test --solution ViajantesTurismo.slnx`.
- Single test project: `dotnet test --project <path-to-csproj>`.
- This repo uses xUnit v3 + Microsoft.Testing.Platform (MTP);
    prefer `--filter-class`, `--filter-method`, `--filter-namespace`, and `--filter-trait`
    over legacy VSTest filters.
- Commit messages must pass `commitlint`; use Conventional Commits, keep the header at or under
    100 characters, and wrap every body line to 100 characters or less.
- Put test-host arguments after `--` when required.
- .NET formatting: `dotnet format`.
- Docs/scripts/spec quality checks: `npm run lint:all`, `npm run lint:all:fix`.

## Aspire operations

This repository is set up to use Aspire. Aspire orchestrates the application and handles
dependencies, build/run orchestration, and resource lifecycle. Resources are defined in
AppHost code (for this repo, see `src/ViajantesTurismo.AppHost`).

### General recommendations

1. Before making changes, run the AppHost and inspect resource state to start from a known baseline.
2. Changes to AppHost model code require a restart to take effect.
3. Make incremental changes and re-run to validate.
4. Use Aspire diagnostics/logging/tracing capabilities before changing code.

### Running the application

- Preferred repository-safe command: `dotnet tool run aspire run`
- Notes from official CLI behavior:
    - `dotnet tool run aspire run` uses the repo-pinned `aspire.cli` local tool from `.config/dotnet-tools.json`.
    - `aspire run` builds the AppHost/resources, starts resources, launches dashboard,
        and prints endpoints.
    - If Aspire CLI is installed globally or via the official install script, `aspire run` also works directly.
    - If multiple AppHosts exist, specify with `--project <path-to-apphost-csproj>`.

### Checking resources and debugging

- Check resource status first.
- If a resource is unhealthy, restart that resource and inspect logs/traces.
- Prioritize diagnostics before code edits:
  1. Structured logs
  2. Console logs
  3. Traces
  4. Trace-structured logs

### Integrations

When adding a new resource/integration:

1. List available integrations first.
2. Prefer integration versions compatible with the repo’s Aspire AppHost SDK.
3. Read integration docs before implementing.

### AppHost and tooling notes

- If AppHost changes do not appear, restart `dotnet tool run aspire run` (or `aspire run` if using a global/script installation).
- Use `aspire update` when intentionally updating AppHost/related Aspire packages.
- Persistent containers can create state issues early in development; prefer resettable local flows.
- Do **not** install/use the obsolete Aspire workload.

### Official documentation

Always prefer official docs:

1. `https://aspire.dev`
2. `https://learn.microsoft.com/dotnet/aspire`
3. `https://nuget.org` (for integration package details)

## Problem-solving behavior

- When hitting a dead end or encountering an unfamiliar rule, tool, or error, **do not assume the cause**.
- Research online (official docs, source code, community forums) before proposing a fix.
- Never make speculative changes to code without understanding the actual rule logic or root cause.
- Ask clarifying questions when unsure rather than guessing.

## Key references

- `README.md`
- `docs/CODING_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/TEST_GUIDELINES.md`
- `docs/CODE_QUALITY.md`
- `docs/ARCHITECTURE_DECISIONS.md`
- `docs/adr/`
