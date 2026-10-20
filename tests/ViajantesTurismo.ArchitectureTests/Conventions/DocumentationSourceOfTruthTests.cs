using System.Text.Json;
using ViajantesTurismo.Documentation;
using SharedKernel.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class DocumentationSourceOfTruthTests
{
    [Fact]
    public void System_overview_should_retain_the_management_security_database_edges()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var configPath = Path.Combine(repositoryRoot, "docs", "architecture", "generated-diagrams.json");
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var systemOverview = config.RootElement.GetProperty("blocks")
            .EnumerateArray()
            .Single(block => block.GetProperty("name").GetString() == "system-overview");

        // Act
        var lines = systemOverview.GetProperty("lines")
            .EnumerateArray()
            .Select(line => line.GetString() ?? string.Empty)
            .ToArray();

        // Assert
        lines.ShouldContain("        securityDatabase[(Management security database)]");
        lines.ShouldContain("    managementWeb -- EF Core SQL and security state --> securityDatabase");
        lines.ShouldContain("    migration -- applies Management security migrations --> securityDatabase");
        lines.ShouldContain("    securityDatabase --> databaseServer");
    }

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
        checkCount.ShouldBe(5);
    }

    [Fact]
    public void API_request_shape_policy_should_be_canonical_and_linked()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var policyPath = Path.Combine(repositoryRoot, "docs", "API_REQUEST_SHAPES.md");
        var documentationIndex = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "README.md"));

        // Act
        var policyExists = File.Exists(policyPath);

        // Assert
        policyExists.ShouldBeTrue("Expected docs/API_REQUEST_SHAPES.md to own the API request-shape policy.");
        var policy = File.ReadAllText(policyPath);
        documentationIndex.ShouldContain("[API request shape policy](API_REQUEST_SHAPES.md)", StringComparison.Ordinal);
        policy.ShouldContain("<!-- api-request-shape-policy:start -->", StringComparison.Ordinal);
        policy.ShouldContain("<!-- api-request-shape-policy:end -->", StringComparison.Ordinal);
    }
}
