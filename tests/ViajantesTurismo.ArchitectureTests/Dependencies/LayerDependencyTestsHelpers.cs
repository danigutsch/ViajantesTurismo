using System.Text.RegularExpressions;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ViajantesTurismo.ArchitectureTests.Dependencies;

internal static partial class LayerDependencyTestsHelpers
{
    public static GivenTypesConjunctionWithDescription TypesInNamespace(string namespaceRoot, string description)
    {
        var pattern = $"^{Regex.Escape(namespaceRoot)}(\\.|$)";
        return Types().That().ResideInNamespaceMatching(pattern).As(description);
    }

    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    public static string[] FindSharedKernelProductReferences(string repositoryRoot)
    {
        return SharedKernelSourceFiles(repositoryRoot)
            .SelectMany(filePath => FindProductReferenceLines(repositoryRoot, filePath))
            .ToArray();
    }

    private static IEnumerable<string> SharedKernelSourceFiles(string repositoryRoot)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src", "SharedKernel");
        var testsRoot = Path.Combine(repositoryRoot, "tests");

        return Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateDirectories(testsRoot, "SharedKernel*", SearchOption.TopDirectoryOnly)
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))))
            .Where(IsSourceFile);
    }

    private static bool IsSourceFile(string filePath)
    {
        var normalizedPath = filePath.Replace(Path.DirectorySeparatorChar, '/');
        return !normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            && !normalizedPath.Contains("/obj/", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindProductReferenceLines(string repositoryRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        return File.ReadLines(filePath)
            .Select((line, index) => new { Line = line, LineNumber = index + 1 })
            .Where(entry => IsProductReference(filePath, entry.Line))
            .Select(entry => $"{relativePath}:{entry.LineNumber}: {entry.Line.Trim()}");
    }

    private static bool IsProductReference(string filePath, string line)
    {
        return filePath.EndsWith(".csproj", StringComparison.Ordinal)
            ? line.Contains("<ProjectReference", StringComparison.Ordinal)
                && line.Contains("ViajantesTurismo", StringComparison.Ordinal)
            : ProductUsingDirectiveRegex().IsMatch(line);
    }

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?ViajantesTurismo(\.|;)")]
    private static partial Regex ProductUsingDirectiveRegex();
}
