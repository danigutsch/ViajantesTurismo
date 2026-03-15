using System.Text.RegularExpressions;
using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class NamingConventionTests
{
    private static readonly string[] SolutionRootNamespaces =
    [
        ArchitectureProvider.Namespaces.Domain,
        ArchitectureProvider.Namespaces.Application,
        ArchitectureProvider.Namespaces.Infrastructure,
        ArchitectureProvider.Namespaces.Api,
        ArchitectureProvider.Namespaces.Contracts,
        ArchitectureProvider.Namespaces.Common
    ];

    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        var offendingTypes = ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsInterface && IsWithinSolution(type.Namespace))
            .Where(type => !type.Name.StartsWith('I'))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected all interfaces to start with 'I', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ContractDtos_Should_End_With_Dto()
    {
        const string contractNamespace = ArchitectureProvider.Namespaces.Contracts;
        var offendingTypes = ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith(contractNamespace, StringComparison.Ordinal))
            .Where(type => type.IsClass)
            .Where(type => !IsStaticClass(type))
            .Where(type => !typeof(Attribute).IsAssignableFrom(type))
            .Where(type => !type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected contract types to end with 'Dto', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void TestClasses_Should_End_With_Tests()
    {
        var testsAssembly = typeof(NamingConventionTests).Assembly;
        var offendingTypes = testsAssembly.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => !type.Name.EndsWith("Tests", StringComparison.Ordinal))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected architecture test classes to end with 'Tests', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void Xunit_Test_Methods_Should_Follow_Underscore_Naming_Convention()
    {
        var repositoryRoot = GetRepositoryRoot();
        var offendingMethods = Directory
            .GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindOffendingXunitMethods(repositoryRoot, path))
            .ToArray();

        Assert.False(
            offendingMethods.Length != 0,
            $"Expected xUnit test methods to follow the underscore naming convention, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingMethods)}");
    }

    [Fact]
    public void Behavior_Feature_Files_Should_Use_A_Recognized_Naming_Style()
    {
        var repositoryRoot = GetRepositoryRoot();
        var behaviorSpecsRoot = Path.Combine(
            repositoryRoot,
            "tests",
            "ViajantesTurismo.Admin.BehaviorTests",
            "specs");

        var offendingFiles = Directory
            .GetFiles(behaviorSpecsRoot, "*.feature", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(relativePath =>
            {
                var fileName = Path.GetFileName(relativePath);
                return !KebabCaseFeatureFileRegex().IsMatch(fileName)
                       && !PascalCaseFeatureFileRegex().IsMatch(fileName);
            })
            .ToArray();

        Assert.False(
            offendingFiles.Length != 0,
            $"Expected behavior feature files to use kebab-case or the tracked legacy PascalCase style, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingFiles)}");
    }

    private static bool IsWithinSolution(string? @namespace)
    {
        if (@namespace is null)
        {
            return false;
        }

        return SolutionRootNamespaces.Any(root => @namespace.StartsWith(root, StringComparison.Ordinal));
    }

    private static string[] FindOffendingXunitMethods(string repositoryRoot, string filePath)
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

    private static bool IsGeneratedTestPath(string path) =>
        path.Contains(@"\bin\", StringComparison.Ordinal)
        || path.Contains(@"\obj\", StringComparison.Ordinal);

    private static bool IsStaticClass(Type type) => type is { IsAbstract: true, IsSealed: true };

    [GeneratedRegex(@"^\s*\[(Fact|Theory)\b", RegexOptions.Compiled)]
    private static partial Regex XunitAttributeRegex();

    [GeneratedRegex(@"^\s*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex XunitMethodRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*(?:_[A-Z0-9][A-Za-z0-9]*)+$", RegexOptions.Compiled)]
    private static partial Regex XunitMethodNamingRegex();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)+\.feature$", RegexOptions.Compiled)]
    private static partial Regex KebabCaseFeatureFileRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]+\.feature$", RegexOptions.Compiled)]
    private static partial Regex PascalCaseFeatureFileRegex();
}
