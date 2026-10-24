using System.Xml.Linq;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiCoverageCollectionTests
{
    [Fact]
    public void Independent_solution_build_preserves_projects_outside_test_dependency_graphs()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx"));
        var independentProjects = solution.Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Where(static path => path.StartsWith("benchmarks/", StringComparison.Ordinal)
                || path.StartsWith("samples/", StringComparison.Ordinal))
            .ToArray();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var qualityGate = workflow.Split("  quality-gate:", StringSplitOptions.None)[1]
            .Split("\n  lint:", StringSplitOptions.None)[0];
        var solutionBuild = workflow.Split("  solution-build:", StringSplitOptions.None)[1]
            .Split("\n  quality-gate:", StringSplitOptions.None)[0];
        var referencedProjects = Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories)
            .SelectMany(static path => XDocument.Load(path).Descendants("ProjectReference")
                .Select(reference => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path).ShouldNotBeNull(),
                    reference.Attribute("Include").ShouldNotBeNull().Value.Replace('\\', Path.DirectorySeparatorChar)))))
            .ToHashSet(StringComparer.Ordinal);

        // Assert
        independentProjects.Length.ShouldBeGreaterThan(0);
        foreach (var project in independentProjects)
        {
            referencedProjects.ShouldNotContain(Path.GetFullPath(Path.Combine(root, project)));
        }
        workflow.ShouldContain("dotnet build ViajantesTurismo.slnx -c Release --no-restore", StringComparison.Ordinal);
        qualityGate.ShouldNotContain("dotnet build ViajantesTurismo.slnx", StringComparison.Ordinal);
        qualityGate.ShouldContain(
            "coverage verify --report artifacts --base \"$BASE_SHA\" --symbols TestResults/symbol-evidence.json", StringComparison.Ordinal);
        solutionBuild.ShouldContain("coverage record-symbols", StringComparison.Ordinal);
        solutionBuild.ShouldNotContain("test-slices", StringComparison.Ordinal);
    }
}
