using System.Xml.Linq;
using ViajantesTurismo.ArchitectureTests.Dependencies;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for the project-to-test-category matrix tests described in docs/COVERAGE_OWNERSHIP.md.</summary>
internal static class TestCoverageMatrixTestsHelpers
{
    private const string ArchitectureTestProject = "ViajantesTurismo.ArchitectureTests";

    /// <summary>One matrix row: source projects that match, and the test projects that may directly own them.</summary>
    private sealed record CategoryRule(string Category, Func<string, bool> Matches, string[] OwnerSuffixes);

    // Order matters: the first matching rule wins.
    private static readonly CategoryRule[] Rules =
    [
        new("Contracts", name => name.Contains(".Contracts.", StringComparison.Ordinal), ["ContractTests"]),
        new("Api", name => name.EndsWith(".ApiService", StringComparison.Ordinal), ["ApiServiceTests"]),
        new("Web", name => name.EndsWith(".Web", StringComparison.Ordinal), ["WebTests"]),
        new("DomainOrApplication",
            name => name.EndsWith(".Domain", StringComparison.Ordinal)
                    || name.EndsWith(".Application", StringComparison.Ordinal),
            ["UnitTests", "BehaviorTests"]),
        new("Infrastructure",
            name => name.EndsWith(".Infrastructure", StringComparison.Ordinal),
            ["UnitTests", "InfrastructureTests", "HostedTests"]),
        new("Library",
            _ => true,
            ["UnitTests", "Tests", "HostedTests", "WebTests", "ApiServiceTests", "ContractTests"])
    ];

    /// <summary>
    /// Projects that do not yet meet the matrix, each with the reason and the lane that gives the closest signal.
    /// The baseline only shrinks: a project that meets the matrix must be removed from it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Baseline = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ViajantesTurismo.Admin.Domain"] =
            "Tested through Admin.UnitTests and Admin.BehaviorTests, which reach it through Admin.Application "
            + "and not by a direct project reference.",
        ["ViajantesTurismo.Branding.AspNetCore"] =
            "Tested through Catalog.ApiServiceTests (BrandingApiEndpointTests) against the Catalog host; "
            + "it has no test project of its own.",
        ["ViajantesTurismo.Catalog.IntegrationEventWorker"] =
            "No worker test project exists; the closest signal is HostedTests and WorkerOwnershipTests. "
            + "Revisit when the worker gains logic of its own."
    };

    /// <summary>Returns the sorted source projects (from the solution) that lack a direct test project of the required category.</summary>
    public static string[] FindUncoveredProjects()
    {
        var root = LayerDependencyTestsHelpers.GetRepositoryRoot();
        var solutionProjects = ReadSolutionProjects(root);
        var sources = solutionProjects.Where(path => path.StartsWith("src/", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToArray();
        var testReferences = solutionProjects.Where(path => path.StartsWith("tests/", StringComparison.Ordinal))
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), References: ReadReferences(root, path)))
            .Where(test => test.Name != ArchitectureTestProject && test.Name.EndsWith("Tests", StringComparison.Ordinal))
            .ToArray();

        return [.. sources.Where(source => !IsCovered(source, testReferences)).Order(StringComparer.Ordinal)];
    }

    private static bool IsCovered(string source, (string Name, string[] References)[] tests)
    {
        var rule = Rules.First(candidate => candidate.Matches(source));

        return tests.Any(test => test.References.Contains(source, StringComparer.Ordinal)
                                 && rule.OwnerSuffixes.Any(suffix => test.Name.EndsWith(suffix, StringComparison.Ordinal)));
    }

    private static string[] ReadSolutionProjects(string root) =>
        [.. XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx"))
            .Descendants("Project")
            .Select(project => (project.Attribute("Path")?.Value ?? string.Empty).Replace('\\', '/'))];

    private static string[] ReadReferences(string root, string projectPath) =>
        [.. XDocument.Load(Path.Combine(root, projectPath))
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                (reference.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))];
}
