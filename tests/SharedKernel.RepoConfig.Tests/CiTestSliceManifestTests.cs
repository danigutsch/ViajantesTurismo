using System.Xml.Linq;

namespace SharedKernel.RepoConfig.Tests;

public sealed class CiTestSliceManifestTests
{
    [Fact]
    public void Every_solution_test_project_is_assigned_to_exactly_one_ci_slice()
    {
        // Arrange
        var repositoryRoot = CiTestSelectionTestProcess.GetRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(repositoryRoot, "ViajantesTurismo.slnx"));
        var expectedProjects = solution
            .Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Where(static path => path.StartsWith("tests/", StringComparison.Ordinal))
            .Where(path => XDocument.Load(Path.Combine(repositoryRoot, path))
                .Descendants("PackageReference")
                .Any(static package => string.Equals(
                    package.Attribute("Include")?.Value,
                    "xunit.v3.mtp-v2",
                    StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var manifestsDirectory = Path.Combine(repositoryRoot, "scripts", "ci-test-slices");

        // Act
        var assignedProjects = Directory.EnumerateFiles(manifestsDirectory, "*.txt")
            .Order(StringComparer.Ordinal)
            .SelectMany(static path => File.ReadAllLines(path))
            .Select(static path => path.Trim().Replace('\\', '/'))
            .Where(static path => path.Length > 0)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        assignedProjects.ShouldBe(expectedProjects);
    }

    [Fact]
    public void OpenApi_contract_projects_share_a_fast_shard_with_the_generation_tool_tests()
    {
        // Arrange
        var repositoryRoot = CiTestSelectionTestProcess.GetRepositoryRoot();
        var manifestPath = Path.Combine(
            repositoryRoot,
            "scripts",
            "ci-test-slices",
            "fast-validation-1.txt");

        // Act
        var assignedProjects = File.ReadAllLines(manifestPath);

        // Assert
        assignedProjects.ShouldContain(
            "tests/ViajantesTurismo.OpenApi.Tool.Tests/ViajantesTurismo.OpenApi.Tool.Tests.csproj");
        assignedProjects.ShouldContain(
            "tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj");
        assignedProjects.ShouldContain(
            "tests/ViajantesTurismo.Catalog.ContractTests/ViajantesTurismo.Catalog.ContractTests.csproj");
        assignedProjects.ShouldContain(
            "tests/ViajantesTurismo.Branding.ContractTests/ViajantesTurismo.Branding.ContractTests.csproj");
    }
}
