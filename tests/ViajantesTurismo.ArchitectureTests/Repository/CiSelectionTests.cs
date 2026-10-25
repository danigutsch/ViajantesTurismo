using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiSelectionTests
{
    [Theory]
    [InlineData("docs/fixture.md", "false", "true", "[]")]
    [InlineData(".github/workflows/ci.yml", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\",\"hosted-tests\"]")]
    [InlineData("tests/ViajantesTurismo.Catalog.UnitTests/Fixture.cs", "true", "false", "[\"fast-validation-1\",\"fast-validation-2\"]")]
    [InlineData("src/ViajantesTurismo.Branding.Infrastructure/Fixture.cs", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\",\"hosted-tests\"]")]
    public async Task Selection_runs_only_the_slices_that_a_change_affects(string path, string buildRequired, string documentationOnly, string matrix)
    {
        // Arrange
        using var fixture = new CiSelectionFixture();

        // Act
        var outputs = await fixture.Select(path, TestContext.Current.CancellationToken);

        // Assert
        outputs["build_required"].ShouldBe(buildRequired);
        outputs["documentation_only"].ShouldBe(documentationOnly);
        outputs["matrix"].ShouldBe(matrix);
    }
}
