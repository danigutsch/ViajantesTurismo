# SharedKernel.Documentation.Tool

Local .NET tool for generated documentation checks and refreshes.

## Usage

```text
dotnet run --project tools/SharedKernel.Documentation.Tool -- generate --config docs/architecture/generated-diagrams.json
dotnet run --project tools/SharedKernel.Documentation.Tool -- generate --config docs/architecture/generated-diagrams.json --check
```

Repository scripts may wrap the command. In this repository, use:

```text
bash scripts/update-architecture-diagrams.sh
bash scripts/update-architecture-diagrams.sh --check
```

## Command behavior

- `generate` refreshes all configured generated Markdown blocks.
- `--check` detects drift and exits non-zero without writing files.
- `--config` points to the repository-owned generator config file.

The tool contains no ViajantesTurismo-specific diagram content. Repository-specific system maps,
labels, and source paths belong in config.
