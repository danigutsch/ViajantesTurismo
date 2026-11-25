using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Verifies that registered integration and system seams identify their tested entrypoints.</summary>
internal static class IntegrationSeamRegisterPolicy
{
    private const string SectionHeading = "## Integration and system test seams";

    /// <summary>Returns missing, undeclared, and mismatched test seam documentation.</summary>
    internal static string[] FindViolations(string repositoryRoot)
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, ".config", "test-taxonomy.json")));
        var registered = from suite in inventory.RootElement.GetProperty("suites").EnumerateArray()
                         from seam in suite.GetProperty("seams").EnumerateArray()
                         let type = Read(seam, "type")
                         where type is "Integration" or "System"
                         select new
                         {
                             Register = Read(suite, "register"),
                             Suite = Read(suite, "path"),
                             Sut = Read(seam, "sut"),
                             Type = type,
                             Dependency = Read(seam, "dependency"),
                         };

        var violations = new List<string>();
        foreach (var group in registered.GroupBy(seam => seam.Register, StringComparer.Ordinal))
        {
            var expected = group.ToDictionary(seam => (
                Suite: Path.GetFileNameWithoutExtension(seam.Suite),
                Sut: Path.GetFileNameWithoutExtension(seam.Sut),
                seam.Type));
            var register = Path.Combine(repositoryRoot, group.Key);
            if (!File.Exists(register))
            {
                violations.Add($"Create the test seam register at {group.Key}.");
                continue;
            }

            foreach (var row in ReadRows(register))
            {
                if (row.Length != 5)
                {
                    violations.Add($"{group.Key} has a test seam row without five columns.");
                    continue;
                }

                var (suite, sut, type) = (row[0], row[1], row[2]);
                if (!expected.Remove((suite, sut, type), out var seam))
                {
                    violations.Add($"{group.Key} has an undeclared or repeated {type} seam for {suite} and {sut}.");
                    continue;
                }

                if (row[3] != seam.Dependency)
                {
                    violations.Add($"{group.Key} should record dependency mode '{seam.Dependency}' for {suite} and {sut}.");
                }

                if (!LinksToTestInSuite(repositoryRoot, register, seam.Suite, row[4]))
                {
                    violations.Add($"{group.Key} should link the entrypoint for {suite} and {sut} to an implemented xUnit test in that suite.");
                }
            }

            foreach (var seam in expected.Keys)
            {
                violations.Add($"{group.Key} should document the {seam.Type} seam for {seam.Suite} and {seam.Sut}.");
            }
        }

        return [.. violations];
    }

    private static IEnumerable<string[]> ReadRows(string register) => File.ReadLines(register)
        .SkipWhile(line => line != SectionHeading)
        .Skip(1)
        .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal))
        .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
        .Select(line => line.Trim('|').Split('|', StringSplitOptions.TrimEntries).Select(cell => cell.Trim('`')).ToArray());

    private static bool LinksToTestInSuite(string repositoryRoot, string register, string suite, string entrypoint)
    {
        var linkStart = entrypoint.IndexOf("](", StringComparison.Ordinal);
        if (!entrypoint.StartsWith('[', StringComparison.Ordinal) || linkStart <= 1 || !entrypoint.EndsWith(')', StringComparison.Ordinal))
        {
            return false;
        }

        var link = entrypoint[(linkStart + 2)..^1];
        if (Path.IsPathRooted(link) || !link.EndsWith("Tests.cs", StringComparison.Ordinal))
        {
            return false;
        }

        var file = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(register) ?? repositoryRoot, link));
        var suiteDirectory = Path.GetDirectoryName(Path.Combine(repositoryRoot, suite)) ?? repositoryRoot;
        var relativeTest = Path.GetRelativePath(suiteDirectory, file);
        if (Path.IsPathRooted(relativeTest)
            || relativeTest.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || !File.Exists(file))
        {
            return false;
        }

        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetRoot();
        return NamingConventionTestsHelpers.FindXunitMethods(root)
            .Any(method => method.Body is { Statements.Count: > 0 } || method.ExpressionBody is not null);
    }

    private static string Read(JsonElement item, string property) => item.GetProperty(property).GetString()
        ?? throw new InvalidOperationException($"Test taxonomy property '{property}' must contain text.");
}
