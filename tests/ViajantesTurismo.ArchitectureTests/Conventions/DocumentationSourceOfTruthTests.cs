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
    public void Platform_service_integration_evaluation_should_document_non_adoption_boundary()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var evaluationPath = Path.Combine(repositoryRoot, "docs", "PLATFORM_SERVICE_INTEGRATION_EVALUATION.md");
        var appHostReadmePath = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.AppHost", "README.md");

        // Act
        var evaluationDocExists = File.Exists(evaluationPath);

        // Assert
        evaluationDocExists.ShouldBeTrue();

        var evaluationText = File.ReadAllText(evaluationPath);
        var appHostReadmeText = File.ReadAllText(appHostReadmePath);

        evaluationText.ShouldContain("This document records the current evaluation posture for Epic #903.", StringComparison.Ordinal);
        evaluationText.ShouldContain("No candidate service is adopted by this document.", StringComparison.Ordinal);
        evaluationText.ShouldContain(
            "Security, privacy, validation, testing, and operations implications must be documented before implementation.",
            StringComparison.Ordinal);
        appHostReadmeText.ShouldContain(
            "Candidate platform-service integrations tracked under Epic #903 are evaluation-only until separately adopted.",
            StringComparison.Ordinal);
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
