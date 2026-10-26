using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiSelectionTests
{
    [Theory]
    [InlineData("docs/fixture.md", "false", "true", "[]")]
    [InlineData("roadmap/items/RM-001-fixture.json", "false", "true", "[]")]
    [InlineData("roadmap/order.json", "false", "true", "[]")]
    [InlineData("roadmap/themes/fixture.json", "false", "true", "[]")]
    [InlineData("roadmap/README.md", "false", "true", "[]")]
    [InlineData("observability/README.md", "false", "true", "[]")]
    [InlineData("LICENSE.txt", "false", "true", "[]")]
    [InlineData("SUPPORT.md", "false", "true", "[]")]
    [InlineData("ViajantesTurismo.code-workspace", "false", "true", "[]")]
    [InlineData("ViajantesTurismo.sln.DotSettings", "false", "true", "[]")]
    [InlineData(".gitignore", "false", "true", "[]")]
    [InlineData("CODEOWNERS", "false", "true", "[]")]
    [InlineData(".mcp.json", "false", "true", "[]")]
    [InlineData("docs/architecture/generated-diagrams.json", "false", "true", "[]")]
    [InlineData("tests/BDD_GUIDE.md", "false", "true", "[]")]
    [InlineData("tests/performance/k6/scenarios/admin-smoke.js", "false", "true", "[]")]
    [InlineData("tests/README.md", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\",\"hosted-tests\"]")]
    [InlineData("docs/asyncapi.json", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\",\"hosted-tests\"]")]
    [InlineData(".devcontainer/devcontainer.json", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\",\"hosted-tests\"]")]
    [InlineData("roadmap/config.json", "true", "false", "[\"fast-validation-2\"]")]
    [InlineData("roadmap/schema/fixture.schema.json", "true", "false", "[\"fast-validation-2\"]")]
    [InlineData("roadmap/reconciliation/fixture.json", "true", "false", "[\"fast-validation-2\"]")]
    [InlineData("observability/otel-collector/aspire.yaml", "true", "false",
        "[\"fast-validation-2\",\"hosted-tests\"]")]
    [InlineData("tests/fixtures/media-accessibility/baseline.json", "true", "false",
        "[\"fast-validation-1\",\"fast-validation-2\"]")]
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
