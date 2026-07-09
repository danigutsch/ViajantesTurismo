using System.Globalization;

namespace SharedKernel.Documentation.Tests;

[Trait(Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.CommandLineCapability)]
public sealed class DocumentationToolApplicationTests
{
    [Fact]
    public async Task Run_returns_error_when_config_is_missing()
    {
        // Arrange
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(["generate"], output, error, Directory.GetCurrentDirectory());
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required --config <path>.", StringComparison.Ordinal);
        errorText.ShouldContain("Usage: sharedkernel-docs generate --config <path> [--check]", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_returns_error_when_config_file_is_missing()
    {
        // Arrange
        using var workspace = new TemporaryDocumentationWorkspace();
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(
            ["generate", "--config", "docs/architecture/generated-diagrams.json"],
            output,
            error,
            workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Documentation generation failed:", StringComparison.Ordinal);
        errorText.ShouldContain("generated-diagrams.json", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_returns_error_when_config_value_is_missing()
    {
        // Arrange
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(["generate", "--config"], output, error, Directory.GetCurrentDirectory());
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required value for --config.", StringComparison.Ordinal);
        errorText.ShouldContain("Usage: sharedkernel-docs generate --config <path> [--check]", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_returns_error_when_config_value_is_another_option()
    {
        // Arrange
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(["generate", "--config", "--check"], output, error, Directory.GetCurrentDirectory());
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required value for --config.", StringComparison.Ordinal);
        errorText.ShouldContain("Usage: sharedkernel-docs generate --config <path> [--check]", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_returns_error_when_command_is_unknown()
    {
        // Arrange
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(["unknown"], output, error, Directory.GetCurrentDirectory());
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown command: unknown", StringComparison.Ordinal);
        errorText.ShouldContain("Usage: sharedkernel-docs generate --config <path> [--check]", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_returns_error_when_argument_is_unknown()
    {
        // Arrange
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(["generate", "--unknown"], output, error, Directory.GetCurrentDirectory());
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown argument: --unknown", StringComparison.Ordinal);
        errorText.ShouldContain("Usage: sharedkernel-docs generate --config <path> [--check]", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_reports_current_when_generated_documentation_is_current()
    {
        // Arrange
        using var workspace = new TemporaryDocumentationWorkspace();
        workspace.WriteArchitectureDoc("overview.md", DocumentationTestContent.GeneratedBlockDocument("old[Old]"));
        workspace.WriteConfig(DocumentationTestContent.GeneratorConfig("newNode[New node]"));
        DocumentationGenerator.Run(workspace.RootPath, "docs/architecture/generated-diagrams.json", checkOnly: false);
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(
            ["generate", "--config", "docs/architecture/generated-diagrams.json", "--check"],
            output,
            error,
            workspace.RootPath);
        var outputText = output.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("Generated documentation is current.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_check_reports_stale_generated_documentation()
    {
        // Arrange
        using var workspace = new TemporaryDocumentationWorkspace();
        workspace.WriteArchitectureDoc("overview.md", DocumentationTestContent.GeneratedBlockDocument("old[Old]"));
        workspace.WriteConfig(DocumentationTestContent.GeneratorConfig("newNode[New node]"));
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(
            ["generate", "--config", "docs/architecture/generated-diagrams.json", "--check"],
            output,
            error,
            workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Generated documentation is stale:", StringComparison.Ordinal);
        errorText.ShouldContain("docs/architecture/overview.md", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_updates_generated_documentation()
    {
        // Arrange
        using var workspace = new TemporaryDocumentationWorkspace();
        workspace.WriteArchitectureDoc("overview.md", DocumentationTestContent.GeneratedBlockDocument("old[Old]"));
        workspace.WriteConfig(DocumentationTestContent.GeneratorConfig("newNode[New node]"));
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await DocumentationToolApplication.Run(
            ["generate", "--config", "docs/architecture/generated-diagrams.json"],
            output,
            error,
            workspace.RootPath);
        var outputText = output.ToString();
        var updated = workspace.ReadArchitectureDoc("overview.md");

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("Updated generated documentation:", StringComparison.Ordinal);
        outputText.ShouldContain("docs/architecture/overview.md", StringComparison.Ordinal);
        updated.ShouldContain("newNode[New node]", StringComparison.Ordinal);
    }
}
