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
}
