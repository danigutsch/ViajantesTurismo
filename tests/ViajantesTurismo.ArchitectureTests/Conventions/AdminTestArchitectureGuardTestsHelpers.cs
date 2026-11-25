using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for the architecture guards that scan Admin test sources for banned test-authoring patterns.</summary>
internal static partial class AdminTestArchitectureGuardTestsHelpers
{
    private static readonly string[] CanonicalTraitNames =
    [
        TestTraitNames.ScopeName,
        TestTraitNames.AreaName,
        TestTraitNames.CapabilityName,
        TestTraitNames.CategoryName,
        TestTraitNames.HostName,
        TestTraitNames.SurfaceName
    ];

    private static readonly string[] ProductSpecificSharedKernelTestingTerms =
    [
        "ViajantesTurismo.",
        "\"bookings\"",
        "\"customers\"",
        "\"tours\"",
        "\"payments\"",
        "\"catalog\"",
        "\"public-web\""
    ];

    private static readonly Regex HardcodedCanonicalTraitNameRegex = new(
        $@"(?:Trait\s*\(\s*""(?:{string.Join('|', CanonicalTraitNames.Select(Regex.Escape))})""|const\s+string\s+\w+Name\s*=\s*""(?:{string.Join('|', CanonicalTraitNames.Select(Regex.Escape))})"")",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Finds public members that expose the service provider or scopes, returned as "path:Lline text" offenses.</summary>
    public static string[] FindGenericServiceProviderReachThrough(string filePath)
    {
        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (!PublicServiceProviderReachThroughRegex().IsMatch(lines[lineIndex]))
            {
                continue;
            }

            offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
        }

        return [.. offenses];
    }

    /// <summary>Finds hardcoded canonical trait names outside raw strings, returned as offenses.</summary>
    public static string[] FindHardcodedCanonicalTraitNames(string filePath)
    {
        if (IsCanonicalTraitNamesFile(filePath))
        {
            return [];
        }

        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();
        var insideRawStringLiteral = false;

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (insideRawStringLiteral)
            {
                insideRawStringLiteral = ToggleRawStringLiteralState(line, insideRawStringLiteral);
                continue;
            }

            if (HardcodedCanonicalTraitNameRegex.IsMatch(line) || RedirectingCanonicalTraitNameRegex().IsMatch(line))
            {
                offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {line.Trim()}");
            }

            insideRawStringLiteral = ToggleRawStringLiteralState(line, insideRawStringLiteral);
        }

        return [.. offenses];
    }

    /// <summary>Finds lines that couple the SharedKernel testing code to product-specific names, returned as offenses.</summary>
    public static string[] FindProductSpecificSharedKernelTestingCoupling(string filePath)
    {
        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (!ProductSpecificSharedKernelTestingTerms.Any(term => line.Contains(term, StringComparison.Ordinal)))
            {
                continue;
            }

            offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {line.Trim()}");
        }

        return [.. offenses];
    }

    /// <summary>Finds raw service-collection, provider or scope plumbing inside Fact and Theory method bodies, returned as offenses.</summary>
    public static string[] FindRawServiceProviderPlumbingInTestMethods(string filePath)
    {
        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath).GetRoot();
        return [.. NamingConventionTestsHelpers.FindXunitMethods(root)
            .Select(static method => (SyntaxNode?)method.Body ?? method.ExpressionBody)
            .OfType<SyntaxNode>()
            .SelectMany(static body => body.DescendantNodes())
            .Where(IsRawServiceProviderOperation)
            .Select(node => $"{relativePath}:L{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {node}")];
    }

    private static bool IsRawServiceProviderOperation(SyntaxNode node) => node switch
    {
        InvocationExpressionSyntax invocation => InvokedMethodName(invocation.Expression) is "BuildServiceProvider" or "CreateScope" or "CreateAsyncScope",
        ObjectCreationExpressionSyntax creation => creation.Type.GetLastToken().ValueText == "ServiceCollection",
        _ => false
    };

    private static string? InvokedMethodName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => null
    };

    /// <summary>Finds serial collection definitions without a nearby SerialTestJustification attribute, returned as offenses.</summary>
    public static string[] FindUndocumentedSerialCollectionDefinitions(string filePath)
    {
        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var trimmedLine = lines[lineIndex].TrimStart();
            if (!trimmedLine.StartsWith('[', StringComparison.Ordinal)
                || !SerialCollectionDefinitionRegex().IsMatch(trimmedLine))
            {
                continue;
            }

            var hasJustification = lines
                .Skip(Math.Max(0, lineIndex - 3))
                .Take(7)
                .Any(static line =>
                {
                    var candidate = line.TrimStart();
                    return candidate.StartsWith('[', StringComparison.Ordinal)
                        && candidate.Contains("SerialTestJustification", StringComparison.Ordinal);
                });

            if (!hasJustification)
            {
                offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
            }
        }

        return [.. offenses];
    }

    /// <summary>Reports whether the path is build output, a feature-file code-behind, or other generated test source.</summary>
    public static bool IsGeneratedTestPath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal)
            || normalizedPath.Contains("/Snapshots/", StringComparison.Ordinal)
            || normalizedPath.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*public\s+.*\b(?:IServiceProvider|IServiceScope|CreateScope|CreateAsyncScope|RunInScope)\b", RegexOptions.Compiled)]
    private static partial Regex PublicServiceProviderReachThroughRegex();

    [GeneratedRegex(@"const\s+string\s+\w+\s*=\s*(?:(?:global::)?PandaBear\.Testing\.)?TestTraitNames\.\w+\s*;", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RedirectingCanonicalTraitNameRegex();

    [GeneratedRegex("\"{3,}", RegexOptions.Compiled)]
    private static partial Regex RawStringDelimiterRegex();

    [GeneratedRegex(@"CollectionDefinition.*DisableParallelization\s*=\s*true", RegexOptions.Compiled)]
    private static partial Regex SerialCollectionDefinitionRegex();

    private static bool IsCanonicalTraitNamesFile(string filePath)
    {
        var parentDirectoryName = Path.GetFileName(Path.GetDirectoryName(filePath));

        return Path.GetFileName(filePath).Equals("TestTraitNames.cs", StringComparison.Ordinal)
            && string.Equals(parentDirectoryName, "SharedKernel.Testing", StringComparison.Ordinal);
    }

    private static bool ToggleRawStringLiteralState(string line, bool insideRawStringLiteral)
    {
        var rawStringDelimiterCount = RawStringDelimiterRegex().Count(line);
        return rawStringDelimiterCount % 2 == 0 ? insideRawStringLiteral : !insideRawStringLiteral;
    }
}
