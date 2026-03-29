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
- Use the scoped instruction files in `.github/instructions/` for domain and test-specific conventions.

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
- Use `dotnet format` for .NET formatting.
- Use `npm run lint:all` or `npm run lint:all:fix` when touching markdown, shell, JSON, or Gherkin files.

## Problem-Solving

- Research unfamiliar rules or failures before changing code.
- Prefer official docs and repository docs over guesses.
- Keep changes small, validate them, and check for scoped instructions before adding new conventions.

## Key References

- `README.md`
- `docs/CODING_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/TEST_GUIDELINES.md`
- `docs/CODE_QUALITY.md`
- `docs/ARCHITECTURE_DECISIONS.md`
