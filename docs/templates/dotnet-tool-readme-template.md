# TOOL_PACKAGE_ID

One-sentence purpose.

This package is a .NET tool. The command name is `COMMAND_NAME`.

## Synopsis

```text
COMMAND_NAME --help
COMMAND_NAME --version
COMMAND_NAME COMMAND [options] [operands]
```

## Install or restore

Local tool manifest:

```text
dotnet tool restore
dotnet tool run COMMAND_NAME -- --help
```

Global tool from a trusted package source:

```text
dotnet tool install --global TOOL_PACKAGE_ID --add-source TRUSTED_PACKAGE_SOURCE
COMMAND_NAME --help
```

## Commands

### `COMMAND`

```text
COMMAND_NAME COMMAND [options] [operands]
```

Describe what the command does, what it reads, and what it writes.

## Options

- `--help`: print help to standard output and exit successfully.
- `--version`: print version to standard output and exit successfully.
- `OPTION`: describe accepted values, defaults, and validation.

## Input

Describe standard input, file input, operands, separators, encoding, and empty-input behavior.

## Output

Describe standard output format. Include a stable schema for JSON or machine-readable output.

## Diagnostics

Diagnostics and validation errors go to standard error.

## Exit codes

- `0`: command succeeded.
- `2`: invalid command, arguments, or input.

## Packaging notes

- `PackAsTool=true` packages the console application as a .NET tool.
- `ToolCommandName=COMMAND_NAME` controls the installed command name.
- `PackageReadmeFile=README.md` includes this usage guide in the NuGet package.
- .NET tools run in full trust; install only from trusted manifests and package sources.

## AOT compatibility

State whether the tool is analyzer-clean for Native AOT and whether the package is distributed as a
standard .NET tool or as separate RID-specific native binaries.

If native binaries are supported, document the exact `dotnet publish -r RID -p:PublishAot=true`
verification command and any repository-specific build properties required for the check.
