using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.TestCoverageMatrixTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class TestCoverageMatrixTests
{
    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
    public void Every_source_project_has_one_test_layer_inventory_row()
    {
        // Arrange
        var solutionProjects = ReadSourceProjectNames();

        // Act
        var documentedProjects = ReadDocumentedProjectNames();

        // Assert
        documentedProjects.ShouldBe(solutionProjects,
            "Keep one row per source project in docs/testing/PROJECT_TEST_COVERAGE.md.");
    }

    [Fact]
    public void Every_source_project_has_a_direct_test_project_of_its_category_or_a_named_baseline_exception()
    {
        // Arrange
        var uncovered = FindUncoveredProjects();

        // Act
        var unexplained = uncovered.Except(Baseline.Keys, StringComparer.Ordinal).ToArray();

        // Assert
        unexplained.ShouldBeEmpty(
            "Add a test project of the required category (see docs/COVERAGE_OWNERSHIP.md) that references each project, " +
            "or add a named, reasoned baseline exception.");
    }

    [Fact]
    public void Baseline_exceptions_name_only_projects_that_still_lack_coverage()
    {
        // Arrange
        var uncovered = FindUncoveredProjects();

        // Act
        var stale = Baseline.Keys.Except(uncovered, StringComparer.Ordinal).ToArray();

        // Assert
        stale.ShouldBeEmpty("Remove baseline exceptions for projects that now meet the matrix.");
    }

    [Fact]
    public void Baseline_exceptions_state_a_reason()
    {
        // Arrange & Act
        var unexplained = Baseline.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key).ToArray();

        // Assert
        unexplained.ShouldBeEmpty();
    }
}
