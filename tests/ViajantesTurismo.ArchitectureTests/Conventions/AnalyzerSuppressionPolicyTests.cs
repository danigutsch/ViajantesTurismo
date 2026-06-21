using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedNoWarnEntries =
    [
        "src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj:CS1591",
        "src/ViajantesTurismo.Management.Web/ViajantesTurismo.Management.Web.csproj:CS1591",
        "tests/Directory.Build.props:CA2007",
        "tests/Directory.Build.props:CA5394",
        "tests/Directory.Build.props:CS1591",
        "tests/Directory.Build.props:S107",
        "tests/ViajantesTurismo.Admin.BehaviorTests/ViajantesTurismo.Admin.BehaviorTests.csproj:CA1812"
    ];

    private static readonly HashSet<string> ApprovedPragmaFiles =
    [
        "src/ViajantesTurismo.Admin.Infrastructure/Migrations/20251114124325_Initial.Designer.cs",
        "src/ViajantesTurismo.Admin.Infrastructure/Migrations/AdminWriteDbContextModelSnapshot.cs",
        "tests/SharedKernel.Testing.Analyzers.Tests/SharedKernelTestingAnalyzerTests.cs"
    ];

    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs"
    ];

    [Fact]
    public void Project_And_Props_NoWarn_Entries_Should_Stay_On_The_Approved_Analyzer_Policy_Allowlist()
    {
        var repositoryRoot = GetRepositoryRoot();
        var noWarnEntries = Directory
            .EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(repositoryRoot, "*.props", SearchOption.AllDirectories))
            .Where(path => !IsIgnoredPath(path))
            .SelectMany(path => FindNoWarnEntries(repositoryRoot, path))
            .ToArray();

        var unapprovedEntries = noWarnEntries
            .Where(entry => !ApprovedNoWarnEntries.Contains(entry))
            .ToArray();

        Assert.True(
            unapprovedEntries.Length == 0,
            $"Expected NoWarn entries to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, unapprovedEntries)}");
    }

    [Fact]
    public void Pragma_Warning_Suppressions_Should_Stay_On_The_Approved_Analyzer_Policy_Allowlist()
    {
        var repositoryRoot = GetRepositoryRoot();
        var filesWithPragmas = Directory
            .GetFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Where(path => PragmaWarningDirectiveRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedPragmaFiles.Contains(path))
            .ToArray();

        Assert.True(
            filesWithPragmas.Length == 0,
            $"Expected pragma warning suppressions to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithPragmas)}");
    }

    [Fact]
    public void SuppressMessage_Attributes_Should_Stay_On_The_Approved_Analyzer_Policy_Allowlist()
    {
        var repositoryRoot = GetRepositoryRoot();
        var filesWithSuppressMessage = Directory
            .GetFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Where(path => SuppressMessageAttributeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedSuppressMessageFiles.Contains(path))
            .ToArray();

        Assert.True(
            filesWithSuppressMessage.Length == 0,
            $"Expected SuppressMessage attributes to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithSuppressMessage)}");
    }

    private static string[] FindNoWarnEntries(string repositoryRoot, string filePath)
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

    private static bool IsIgnoredPath(string path)
    {
        return ContainsDirectorySegment(path, "bin")
            || ContainsDirectorySegment(path, "obj")
            || ContainsDirectorySegment(path, ".git")
            || ContainsDirectorySegment(path, ".nuget")
            || ContainsDirectorySegment(path, ".worktrees")
            || path.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    private static bool ContainsDirectorySegment(string path, string directoryName)
    {
        return path.Contains(
            $"{Path.DirectorySeparatorChar}{directoryName}{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
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

    [GeneratedRegex(@"<NoWarn>\s*([^<]+)</NoWarn>", RegexOptions.CultureInvariant)]
    private static partial Regex NoWarnRegex();

    [GeneratedRegex(@"^\s*#pragma\s+warning\s+(?:disable|restore)\b", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex PragmaWarningDirectiveRegex();

    [GeneratedRegex(@"^\s*\[\s*(?:assembly:\s*)?SuppressMessage\s*\(", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SuppressMessageAttributeRegex();
}
