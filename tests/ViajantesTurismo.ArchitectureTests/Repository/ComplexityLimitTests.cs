using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class ComplexityLimitTests
{
    [Fact]
    public void Cyclomatic_complexity_is_limited_by_an_enabled_rule_and_a_wired_threshold()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();

        // Act
        var metrics = File.ReadAllText(Path.Combine(root, "CodeMetricsConfig.txt"));
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        var buildProps = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));

        // Assert
        metrics.ShouldContain("CA1502: 50", StringComparison.Ordinal);
        editorConfig.ShouldContain("dotnet_diagnostic.CA1502.severity = warning", StringComparison.Ordinal);
        buildProps.ShouldContain("CodeMetricsConfig.txt", StringComparison.Ordinal);
    }
}
