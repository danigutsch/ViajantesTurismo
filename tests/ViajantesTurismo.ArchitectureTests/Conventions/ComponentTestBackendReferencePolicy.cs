using System.Text.Json;
using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks project-reference boundaries for registered component test suites.</summary>
internal static class ComponentTestBackendReferencePolicy
{
    /// <summary>Finds undeclared backend hosts in the registered component suite reference chains.</summary>
    internal static string[] FindViolations(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var inventoryPath = Path.Combine(repositoryRoot, ".config", "test-taxonomy.json");
        using var inventory = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        return [.. inventory.RootElement.GetProperty("suites").EnumerateArray()
            .SelectMany(suite => FindSuiteViolations(repositoryRoot, suite))];
    }

    private static IEnumerable<string> FindSuiteViolations(string repositoryRoot, JsonElement suite)
    {
        var seams = suite.GetProperty("seams").EnumerateArray().ToArray();
        if (!seams.Any(seam => seam.GetProperty("type").GetString() == "Component"))
        {
            yield break;
        }

        var suitePath = ReadProjectPath(suite, "path");
        var registeredHosts = seams.Where(IsHostSeam)
            .Select(seam => ReadProjectPath(seam, "sut"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { suitePath };
        var pending = new Queue<(string Project, string[] Path)>();
        pending.Enqueue((suitePath, [suitePath]));

        while (pending.TryDequeue(out var next))
        {
            foreach (var reference in ReadProjectReferences(repositoryRoot, next.Project))
            {
                if (!visited.Add(reference))
                {
                    continue;
                }

                string[] path = [.. next.Path, reference];
                if (IsBackendHost(reference) && !registeredHosts.Contains(reference))
                {
                    yield return $"{string.Join(" -> ", path)}. Use a fake backend client for component tests "
                        + "or register the separate API, integration, or system seam in .config/test-taxonomy.json.";
                    continue;
                }

                pending.Enqueue((reference, path));
            }
        }
    }

    private static IEnumerable<string> ReadProjectReferences(string repositoryRoot, string project)
    {
        var projectPath = Path.Combine(repositoryRoot, project);
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path has no directory: {projectPath}");
        foreach (var reference in XDocument.Load(projectPath).Descendants("ProjectReference"))
        {
            var include = reference.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var target = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', '/')));
            var relativePath = Path.GetRelativePath(repositoryRoot, target).Replace('\\', '/');
            if (!Path.IsPathRooted(relativePath) && !relativePath.StartsWith("../", StringComparison.Ordinal))
            {
                yield return relativePath;
            }
        }
    }

    private static bool IsHostSeam(JsonElement seam)
    {
        var sut = ReadProjectPath(seam, "sut");
        return seam.GetProperty("type").GetString() switch
        {
            "Api" => IsApiHost(sut),
            "Integration" or "System" => IsBackendHost(sut),
            _ => false,
        };
    }

    private static bool IsBackendHost(string path)
    {
        return path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
            && (IsApiHost(path) || Path.GetFileName(path).Equals("ViajantesTurismo.AppHost.csproj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsApiHost(string path)
    {
        return path.EndsWith(".Api.csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadProjectPath(JsonElement element, string property)
    {
        return element.GetProperty(property).GetString()
            ?? throw new InvalidOperationException($"Missing test taxonomy project path '{property}'.");
    }
}
