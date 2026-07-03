using System.Text.RegularExpressions;
using System.Xml.Linq;
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

    public static string[] FindLayerAdapterPackageReferences(string repositoryRoot)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        return Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(IsDomainApplicationOrContractProject)
            .SelectMany(filePath => FindAdapterPackageReferences(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindProviderNeutralSharedKernelAdapterPackageReferences(string repositoryRoot)
    {
        var sharedKernelRoot = Path.Combine(repositoryRoot, "src", "SharedKernel");

        return Directory.EnumerateFiles(sharedKernelRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(IsProviderNeutralSharedKernelProject)
            .SelectMany(filePath => FindAdapterPackageReferences(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindSharedKernelEntityFrameworkCoreAdapterNamingViolations(string repositoryRoot)
    {
        var sharedKernelRoot = Path.Combine(repositoryRoot, "src", "SharedKernel");

        return Directory.EnumerateFiles(sharedKernelRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(ReferencesEntityFrameworkCorePackage)
            .Where(filePath => !NamesEntityFrameworkCoreAdapter(filePath))
            .Select(filePath => Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
    }

    private static bool NamesEntityFrameworkCoreAdapter(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return projectName.EndsWith(".EntityFrameworkCore", StringComparison.Ordinal)
            || projectName.Contains(".EntityFrameworkCore.", StringComparison.Ordinal);
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

    private static bool IsDomainApplicationOrContractProject(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        return fileName.EndsWith(".Domain", StringComparison.Ordinal)
            || fileName.EndsWith(".Application", StringComparison.Ordinal)
            || fileName.EndsWith(".Contracts", StringComparison.Ordinal);
    }

    private static bool IsProviderNeutralSharedKernelProject(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return projectName.StartsWith("SharedKernel.", StringComparison.Ordinal)
            && !projectName.Contains(".Npgsql", StringComparison.Ordinal)
            && !projectName.Contains(".EntityFrameworkCore", StringComparison.Ordinal)
            && !projectName.Contains(".Dapper", StringComparison.Ordinal)
            && !projectName.Contains(".Azure", StringComparison.Ordinal)
            && !projectName.Contains(".Redis", StringComparison.Ordinal)
            && !projectName.Contains(".CloudEvents", StringComparison.Ordinal)
            && !projectName.Contains(".Aspire.Hosting", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindAdapterPackageReferences(string repositoryRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        var document = XDocument.Load(filePath);

        return document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(packageName => packageName is not null && IsAdapterPackage(packageName))
            .Select(packageName => $"{relativePath}: PackageReference Include=\"{packageName}\"");
    }

    private static bool ReferencesEntityFrameworkCorePackage(string filePath)
    {
        var document = XDocument.Load(filePath);

        return document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Any(packageName => packageName is not null && packageName.Contains("EntityFrameworkCore", StringComparison.Ordinal));
    }

    private static bool IsAdapterPackage(string packageName)
    {
        return packageName.Equals("Npgsql", StringComparison.Ordinal)
            || packageName.StartsWith("Npgsql.", StringComparison.Ordinal)
            || packageName.Contains("EntityFrameworkCore", StringComparison.Ordinal)
            || packageName.Equals("Dapper", StringComparison.Ordinal)
            || packageName.StartsWith("Azure.", StringComparison.Ordinal)
            || packageName.StartsWith("Aspire.Npgsql", StringComparison.Ordinal)
            || packageName.StartsWith("Aspire.StackExchange.Redis", StringComparison.Ordinal)
            || packageName.StartsWith("StackExchange.Redis", StringComparison.Ordinal)
            || packageName.StartsWith("RabbitMQ.", StringComparison.Ordinal)
            || packageName.StartsWith("MassTransit", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(?:(static\s+)?(global::)?ViajantesTurismo(\.|;)|[A-Za-z_][A-Za-z0-9_]*\s*=\s*(global::)?ViajantesTurismo(\.|;))")]
    private static partial Regex ProductUsingDirectiveRegex();
}
