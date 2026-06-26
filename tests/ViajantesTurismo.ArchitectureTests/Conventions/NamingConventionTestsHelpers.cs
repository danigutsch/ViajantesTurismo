using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class NamingConventionTestsHelpers
{
    public static bool IsWithinSolution(string? @namespace, IReadOnlyCollection<string> solutionRootNamespaces)
    {
        if (@namespace is null)
        {
            return false;
        }

        return solutionRootNamespaces.Any(root => @namespace.StartsWith(root, StringComparison.Ordinal));
    }

    public static string[] FindOffendingXunitMethods(string repositoryRoot, string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var offendingMethods = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (!XunitAttributeRegex().IsMatch(lines[lineIndex]))
            {
                continue;
            }

            for (var candidateIndex = lineIndex + 1; candidateIndex < Math.Min(lineIndex + 6, lines.Length); candidateIndex++)
            {
                var match = XunitMethodRegex().Match(lines[candidateIndex]);
                if (!match.Success)
                {
                    continue;
                }

                var methodName = match.Groups[1].Value;
                if (!XunitMethodNamingRegex().IsMatch(methodName))
                {
                    offendingMethods.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{candidateIndex + 1} {methodName}");
                }

                break;
            }
        }

        return [.. offendingMethods];
    }

    public static string[] FindOffendingAssertionMethodCalls(string repositoryRoot, string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var offendingAssertions = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (!SimpleAssertMethodCallRegex().IsMatch(lines[lineIndex]))
            {
                continue;
            }

            offendingAssertions.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
        }

        return [.. offendingAssertions];
    }

    public static bool IsStaticClass(Type type) => type is { IsAbstract: true, IsSealed: true };

    public static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(solutionPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    public static bool IsGeneratedTestPath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');

        return normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)+\.feature$", RegexOptions.Compiled)]
    public static partial Regex KebabCaseFeatureFileRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]+\.feature$", RegexOptions.Compiled)]
    public static partial Regex PascalCaseFeatureFileRegex();

    [GeneratedRegex(@"^\s*\[(Fact|Theory)\b", RegexOptions.Compiled)]
    private static partial Regex XunitAttributeRegex();

    [GeneratedRegex(@"^\s*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex XunitMethodRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9][A-Za-z0-9]*)+$", RegexOptions.Compiled)]
    private static partial Regex XunitMethodNamingRegex();

    [GeneratedRegex(@"Assert\.(Equal|Null|NotNull|True|False)\([^\n]*\.[A-Za-z_][A-Za-z0-9_]*\([^\n]*\)", RegexOptions.Compiled)]
    private static partial Regex SimpleAssertMethodCallRegex();
}
