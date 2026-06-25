using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs"
    ];

    [Fact]
    public void Project_And_Props_Should_Not_Use_NoWarn_Entries()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var noWarnEntries = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .SelectMany(path => FindNoWarnEntries(repositoryRoot, path))
            .ToArray();

        Assert.True(
            noWarnEntries.Length == 0,
            $"Expected project and props files not to use NoWarn entries, but found:{Environment.NewLine}{string.Join(Environment.NewLine, noWarnEntries)}");
    }

    [Fact]
    public void Hand_Written_Source_Should_Not_Use_Pragma_Warning_Suppressions()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithPragmas = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsGeneratedSource(repositoryRoot, path))
            .Where(ContainsPragmaWarningDirective)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();

        Assert.True(
            filesWithPragmas.Length == 0,
            $"Expected hand-written source not to use pragma warning suppressions, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithPragmas)}");
    }

    [Fact]
    public void SuppressMessage_Attributes_Should_Stay_On_The_Approved_Analyzer_Policy_Allowlist()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithSuppressMessage = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => SuppressMessageAttributeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedSuppressMessageFiles.Contains(path))
            .ToArray();

        Assert.True(
            filesWithSuppressMessage.Length == 0,
            $"Expected SuppressMessage attributes to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithSuppressMessage)}");
    }

    internal static string[] FindNoWarnEntries(string repositoryRoot, string filePath)
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

    internal static bool ContainsPragmaWarningDirective(string filePath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
        return syntaxTree.GetRoot()
            .DescendantTrivia(descendIntoTrivia: true)
            .Any(static trivia => trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax);
    }

    [GeneratedRegex(@"<NoWarn(?:\s+[^>]*)?>\s*([^<]+)</NoWarn>", RegexOptions.CultureInvariant)]
    internal static partial Regex NoWarnRegex();

    [GeneratedRegex(
        @"^\s*\[\s*(?:assembly:\s*)?(?:(?:global::)?System\.Diagnostics\.CodeAnalysis\.)?SuppressMessage(?:Attribute)?\s*\(",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    internal static partial Regex SuppressMessageAttributeRegex();

    internal static class AnalyzerSuppressionPolicyTestsHelpers
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
}
