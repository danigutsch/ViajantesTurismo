# Project Guidelines

## Repository-Wide Rules

- Follow `docs/CODING_GUIDELINES.md` and `.editorconfig`.
- Use file-scoped namespaces, 4-space indentation, nullable reference types, and XML documentation on public APIs.
- Treat all warnings as errors (`Directory.Build.props`).
- Do not append the `Async` suffix to method names.

## Architecture

- Respect Clean Architecture boundaries:
    - Domain: `src/ViajantesTurismo.Admin.Domain`
    - Application: `src/ViajantesTurismo.Admin.Application`
    - Infrastructure: `src/ViajantesTurismo.Admin.Infrastructure`
    - API: `src/ViajantesTurismo.Admin.ApiService`
- Keep business rules in the Domain layer; do not move them into API or infrastructure code.
- Read the nearest `AGENTS.md` file for scoped guidance on specific areas of work (see **Scoped Agent Instructions** below).

## Build and Validation

- Set up the repo with `setup-dev.ps1` (Windows) or `setup-dev.sh` (Unix).
- Use the .NET SDK from `global.json` and the Node.js/npm versions from `package.json`.
- Build: `dotnet build ViajantesTurismo.slnx`
- Run the app: `dotnet run --project src/ViajantesTurismo.AppHost`
- Run all tests: `dotnet test --solution ViajantesTurismo.slnx`
- Run one test project: `dotnet test --project <path-to-csproj>`
- This repo uses xUnit v3 with Microsoft.Testing.Platform (MTP);
    prefer `--filter-class`, `--filter-method`, `--filter-namespace`, and `--filter-trait`
    over legacy VSTest filters.
- Commit messages must pass `commitlint`; use Conventional Commits, keep the header at or under
    100 characters, and wrap every body line to 100 characters or less.
- Use `dotnet format` for .NET formatting.
- Use `npm run lint:all` or `npm run lint:all:fix` when touching markdown, shell, JSON, or Gherkin files.

## Problem-Solving

- Research unfamiliar rules or failures before changing code.
- Prefer official docs and repository docs over guesses.
- Keep changes small, validate them, and check for scoped instructions before adding new conventions.

## Scoped Agent Instructions

Read the nearest `AGENTS.md` file before working in each area:

| Scope | File |
| --- | --- |
| Repository-wide guidance | [AGENTS.md](../AGENTS.md) |
| .github files and automation assets | [.github/AGENTS.md](.github/AGENTS.md) |
| AppHost orchestration | [src/ViajantesTurismo.AppHost/AGENTS.md](../src/ViajantesTurismo.AppHost/AGENTS.md) |
| Application layer | [src/ViajantesTurismo.Admin.Application/AGENTS.md](../src/ViajantesTurismo.Admin.Application/AGENTS.md) |
| Contracts layer | [src/ViajantesTurismo.Admin.Contracts/AGENTS.md](../src/ViajantesTurismo.Admin.Contracts/AGENTS.md) |
| Domain entities, aggregates, value objects | [src/ViajantesTurismo.Admin.Domain/AGENTS.md](../src/ViajantesTurismo.Admin.Domain/AGENTS.md) |
| Infrastructure layer | [src/ViajantesTurismo.Admin.Infrastructure/AGENTS.md](../src/ViajantesTurismo.Admin.Infrastructure/AGENTS.md) |
| Web layer and Razor components | [src/ViajantesTurismo.Admin.Web/AGENTS.md](../src/ViajantesTurismo.Admin.Web/AGENTS.md) |
| Test guidance (all tests) | [tests/AGENTS.md](../tests/AGENTS.md) |
| BDD/Reqnroll behavior tests | [tests/ViajantesTurismo.Admin.BehaviorTests/AGENTS.md](../tests/ViajantesTurismo.Admin.BehaviorTests/AGENTS.md) |
| Integration tests (API/infrastructure) | [tests/ViajantesTurismo.Admin.IntegrationTests/AGENTS.md](../tests/ViajantesTurismo.Admin.IntegrationTests/AGENTS.md) |
| E2E tests (browser automation) | [tests/ViajantesTurismo.Admin.E2ETests/AGENTS.md](../tests/ViajantesTurismo.Admin.E2ETests/AGENTS.md) |

## Key References

- `README.md`
- `docs/CODING_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/TEST_GUIDELINES.md`
- `docs/CODE_QUALITY.md`
- `docs/ARCHITECTURE_DECISIONS.md`
