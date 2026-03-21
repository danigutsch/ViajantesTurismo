# Project Guidelines

## Code Style

- Follow `docs/CODING_GUIDELINES.md` and `.editorconfig` for C# formatting and naming.
- Use file-scoped namespaces, 4-space indentation, and nullable reference types.
- Do not append the `Async` suffix to method names; use clear method names without the suffix, including for asynchronous methods.
- Treat all warnings as errors (`Directory.Build.props` enforces this); avoid introducing analyzer warnings.
- Prefer explicit domain types for clarity and `var` only when type is obvious.
- Public APIs should include XML documentation.

## Architecture

- Respect Clean Architecture boundaries:
    - Domain: `src/ViajantesTurismo.Admin.Domain`
    - Application: `src/ViajantesTurismo.Admin.Application`
    - Infrastructure: `src/ViajantesTurismo.Admin.Infrastructure`
    - API: `src/ViajantesTurismo.Admin.ApiService`
- Keep domain logic in aggregates/value objects; do not move business rules into API or infrastructure layers.
- Domain operations should use `Result` / `Result<T>` (not exceptions for expected validation failures).
- Enforce aggregate boundaries (for example, booking lifecycle flows through `Tour`).
- Use `ContractConstants` (`src/ViajantesTurismo.Admin.Contracts/ContractConstants.cs`) for shared validation limits.

## Build and Test

- Prerequisites and environment setup:
    - `setup-dev.ps1` (Windows) or `setup-dev.sh` (Unix)
    - .NET SDK from `global.json`
    - Node.js per `package.json` engines
- Core commands:
    - Build: `dotnet build ViajantesTurismo.slnx`
    - Run app: `dotnet run --project src/ViajantesTurismo.AppHost`
    - Run all tests: `dotnet test --solution ViajantesTurismo.slnx`
    - Run one test project: `dotnet test --project <path-to-csproj>`
- Tests should be independent and should not depend on pre-seeded data where possible.
- This repo uses xUnit v3 with Microsoft.Testing.Platform (MTP):
    - Prefer xUnit filter switches such as `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`
    - Do not use legacy VSTest `--filter "FullyQualifiedName~..."` syntax
    - Coverage/test-host options go after `--` when required by command shape

## Conventions

- Follow domain validation guidance in `docs/DOMAIN_VALIDATION.md`:
    - Factory methods + private constructors for entities
    - Dedicated `*Errors` classes per aggregate
    - Application layer handles persistence-dependent invariants (for example uniqueness)
- Follow test naming and patterns in `docs/TEST_GUIDELINES.md`:
    - Naming: `Method_Name_Context_Description_Expected_Behavior`
    - Prefer AAA structure in tests
- Keep architectural decisions aligned with ADRs in `docs/ARCHITECTURE_DECISIONS.md` and `docs/adr/`.

## Quality Checks

- Use `dotnet format` for .NET formatting.
- Use npm quality scripts for docs/scripts/specs:
    - `npm run lint:all`
    - `npm run lint:all:fix`
- Linting is separate from `dotnet build`; run relevant checks when touching markdown, shell, JSON, or Gherkin files.
- When generating or suggesting commit messages, use Conventional Commits in the form
    `<type>[optional scope]: <description>`.
- Allowed commit types: `feat`, `fix`, `docs`, `ci`, `build`, `test`, `refactor`, `perf`, `style`, `chore`, `revert`.

## Key References

- `README.md`
- `docs/CODING_GUIDELINES.md`
- `docs/TEST_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/CODE_QUALITY.md`
- `docs/ARCHITECTURE_DECISIONS.md`
