using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class NamingConventionTestsHelpers
{
    public static bool IsWithinSolution(string? @namespace, IReadOnlyCollection<string> solutionRootNamespaces)
    {
        return @namespace is not null
            && solutionRootNamespaces.Any(root => @namespace.StartsWith(root, StringComparison.Ordinal));
    }

    public static string[] FindOffendingXunitMethods(string repositoryRoot, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath).GetRoot();
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        var offendingMethods = new List<string>();
        var xunitAliases = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(static directive => directive.Alias is not null && directive.Name is not null)
            .Where(static directive => IsQualifiedXunitAttribute(directive.Name?.ToString()))
            .Select(static directive => directive.Alias?.Name.Identifier.ValueText)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var isXunitMethod = method.AttributeLists
                .SelectMany(static list => list.Attributes)
                .Any(attribute => IsXunitAttribute(attribute.Name, xunitAliases));
            var methodName = method.Identifier.ValueText;
            if (!isXunitMethod || XunitMethodNamingRegex().IsMatch(methodName))
            {
                continue;
            }

            var lineNumber = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            offendingMethods.Add($"{relativePath}:L{lineNumber} {methodName}");
        }

        return [.. offendingMethods];
    }

    private static bool IsXunitAttribute(NameSyntax name, HashSet<string> aliases)
    {
        var attributeName = name.ToString();
        return aliases.Contains(attributeName)
            || attributeName is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute"
            || IsQualifiedXunitAttribute(attributeName);
    }

    private static bool IsQualifiedXunitAttribute(string? name)
    {
        var normalizedName = name?.Replace("global::", string.Empty, StringComparison.Ordinal);
        return normalizedName is "Xunit.Fact"
            or "Xunit.FactAttribute"
            or "Xunit.Theory"
            or "Xunit.TheoryAttribute";
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
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal)
            || normalizedPath.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)+\.feature$", RegexOptions.Compiled)]
    public static partial Regex KebabCaseFeatureFileRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]+\.feature$", RegexOptions.Compiled)]
    public static partial Regex PascalCaseFeatureFileRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9][A-Za-z0-9]*)+$", RegexOptions.Compiled)]
    private static partial Regex XunitMethodNamingRegex();

    [GeneratedRegex(@"Assert\.(Equal|Null|NotNull|True|False)\([^\n]*\.[A-Za-z_][A-Za-z0-9_]*\([^\n]*\)", RegexOptions.Compiled)]
    private static partial Regex SimpleAssertMethodCallRegex();
}
