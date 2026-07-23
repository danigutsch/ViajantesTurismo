using SharedKernel.Documentation;
using SharedKernel.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class DocumentationSourceOfTruthTests
{
    [Fact]
    public void Documentation_conformance_should_match_source_backed_facts()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var checkCount = DocumentationConformanceChecker.Check(
            repositoryRoot,
            "docs/architecture/documentation-conformance.json");

        // Assert
        checkCount.ShouldBe(4);
    }
}
