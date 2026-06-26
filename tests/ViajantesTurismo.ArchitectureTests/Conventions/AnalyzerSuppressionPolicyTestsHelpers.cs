using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class AnalyzerSuppressionPolicyTestsHelpers
{
    public static IEnumerable<string> EnumerateRepositoryFiles(string repositoryRoot, string searchPattern)
    {
        return EnumerateRepositoryFiles(new DirectoryInfo(repositoryRoot), searchPattern);
    }

    public static bool IsIgnoredPath(string path)
    {
        return ContainsDirectorySegment(path, "bin")
            || ContainsDirectorySegment(path, "obj")
            || ContainsDirectorySegment(path, ".git")
            || ContainsDirectorySegment(path, ".nuget")
            || path.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    public static bool IsGeneratedSource(string repositoryRoot, string path)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

        return relativePath.Contains("/Migrations/", StringComparison.Ordinal)
            && (relativePath.EndsWith(".Designer.cs", StringComparison.Ordinal)
                || relativePath.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));
    }

    public static string[] FindNoWarnEntries(string repositoryRoot, string filePath)
    {
        var text = File.ReadAllText(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        var entries = new List<string>();

        foreach (Match match in NoWarnRegex().Matches(text))
        {
            var diagnostics = match.Groups[1].Value
                .Replace("$(NoWarn)", string.Empty, StringComparison.Ordinal)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => $"{relativePath}:{value}");

            entries.AddRange(diagnostics);
        }

        return [.. entries];
    }

    public static bool ContainsPragmaWarningDirective(string filePath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
        return syntaxTree.GetRoot()
            .DescendantTrivia(descendIntoTrivia: true)
            .Any(static trivia => trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax);
    }

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

    [GeneratedRegex(@"<NoWarn(?:\s+[^>]*)?>\s*([^<]+)</NoWarn>", RegexOptions.CultureInvariant)]
    public static partial Regex NoWarnRegex();

    [GeneratedRegex(
        @"^\s*\[\s*(?:assembly:\s*)?(?:(?:global::)?System\.Diagnostics\.CodeAnalysis\.)?SuppressMessage(?:Attribute)?\s*\(",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    public static partial Regex SuppressMessageAttributeRegex();

    private static IEnumerable<string> EnumerateRepositoryFiles(DirectoryInfo directory, string searchPattern)
    {
        foreach (var file in directory.EnumerateFiles(searchPattern))
        {
            yield return file.FullName;
        }

        foreach (var childDirectory in directory.EnumerateDirectories())
        {
            if (IsIgnoredDirectoryName(childDirectory.Name))
            {
                continue;
            }

            foreach (var file in EnumerateRepositoryFiles(childDirectory, searchPattern))
            {
                yield return file;
            }
        }
    }

    private static bool IsIgnoredDirectoryName(string directoryName)
    {
        return directoryName is "bin" or "obj" or ".git" or ".nuget" or ".worktrees";
    }

    private static bool ContainsDirectorySegment(string path, string directoryName)
    {
        return path.Contains(
            $"{Path.DirectorySeparatorChar}{directoryName}{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);
    }
}
