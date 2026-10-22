using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiCoverageCollectionTests
{
    [Theory(Skip = "The Bash coverage collector runs on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Collectors_preserve_reports_with_identical_timestamps(bool explicitProjects)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        workspace.WriteFile("scripts/ci-test-slices/first.txt", "tests/First.Tests/First.Tests.csproj\n");
        workspace.WriteFile("scripts/ci-test-slices/second.txt", "tests/Second.Tests/Second.Tests.csproj\n");
        workspace.WriteFile("collector-probe.sh", """
            dotnet() {
                local project=solution results=
                while [[ $# -gt 0 ]]; do
                    case "$1" in
                        --project) project="$2"; shift ;;
                        --results-directory) results="$2"; shift ;;
                    esac
                    shift
                done
                mkdir -p "$results"
                printf '%s' "$project" > "$results/coverage.cobertura.fixed-timestamp.xml"
            }
            export -f dotnet
            bash "$@"
            """);
        var arguments = new List<string> { "collector-probe.sh", Path.Combine(root, "scripts/collect-test-coverage.sh"), "reports.txt" };
        if (explicitProjects)
        {
            arguments.AddRange(["tests/First.Tests/First.Tests.csproj", "tests/Second.Tests/Second.Tests.csproj"]);
        }

        // Act
        var result = await CiTestSelectionTestProcess.Run("bash", workspace.RootPath, arguments,
            TestContext.Current.CancellationToken, new Dictionary<string, string?> { ["CI_TEST_PROJECT_PARALLELISM"] = "2" });

        // Assert
        result.ExitCode.ShouldBe(0);
        var reports = workspace.ReadFile("reports.txt").Trim().Split(';');
        reports.Length.ShouldBe(2);
        reports.Distinct(StringComparer.Ordinal).Count().ShouldBe(2);
        var projects = reports.Select(path => workspace.ReadFile(path)).Order(StringComparer.Ordinal).ToArray();
        projects.ShouldBe(["tests/First.Tests/First.Tests.csproj", "tests/Second.Tests/Second.Tests.csproj"]);
    }

    [Fact]
    public void Independent_solution_build_preserves_projects_outside_test_dependency_graphs()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
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
        qualityGate.ShouldContain("verify-build-evidence", StringComparison.Ordinal);
        solutionBuild.ShouldNotContain("test-slices", StringComparison.Ordinal);
    }
}
