using SharedKernel.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.DocumentationSourceOfTruthTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class DocumentationSourceOfTruthTests
{
    [Fact]
    public void Documentation_index_should_identify_required_canonical_sources()
    {
        // Arrange
        var docsReadme = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "docs", "README.md"));

        // Act
        var missingRows = RequiredSourceOfTruthRows
            .Where(row => !docsReadme.Contains(row, StringComparison.Ordinal))
            .ToArray();
        var hasDeprecatedDocsStatus = docsReadme.Contains("Deprecated docs: none identified.", StringComparison.Ordinal);
        var hasCentralizedGuidanceStatus = docsReadme.Contains("Centralized guidance:", StringComparison.Ordinal);

        // Assert
        missingRows.ShouldBe([]);
        hasDeprecatedDocsStatus.ShouldBeTrue();
        hasCentralizedGuidanceStatus.ShouldBeTrue();
    }

    [Fact]
    public void Generated_architecture_diagram_markers_should_exist_once()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var violations = FindGeneratedArchitectureMarkerViolations(repositoryRoot);

        // Assert
        violations.ShouldBe([]);
    }
}
