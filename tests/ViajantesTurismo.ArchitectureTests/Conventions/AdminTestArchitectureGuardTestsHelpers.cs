using System.Text.RegularExpressions;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for the architecture guards that scan Admin test sources for banned test-authoring patterns.</summary>
internal static partial class AdminTestArchitectureGuardTestsHelpers
{
    private static readonly string[] CanonicalTraitNames =
    [
        TestTraitNames.ScopeName,
        TestTraitNames.AreaName,
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
        var repositoryRoot = GetRepositoryRoot();
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

    /// <summary>Finds hardcoded canonical trait names (scope, area, category, host, surface) outside raw strings, returned as offenses.</summary>
    public static string[] FindHardcodedCanonicalTraitNames(string filePath)
    {
        if (IsCanonicalTraitNamesFile(filePath))
        {
            return [];
        }

        var repositoryRoot = GetRepositoryRoot();
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
        var repositoryRoot = GetRepositoryRoot();
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
        var repositoryRoot = GetRepositoryRoot();
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();
        var insideTestMethod = false;
        var awaitingMethodSignature = false;
        var braceDepth = 0;

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var trimmedLine = line.TrimStart();

            if (!insideTestMethod && !awaitingMethodSignature && IsTestAttributeLine(trimmedLine))
            {
                awaitingMethodSignature = true;
                continue;
            }

            if (awaitingMethodSignature && TestMethodSignatureRegex().IsMatch(trimmedLine))
            {
                insideTestMethod = true;
                awaitingMethodSignature = false;
                braceDepth = CountBraceDelta(line);
                continue;
            }

            if (!insideTestMethod)
            {
                continue;
            }

            if (RawServiceProviderPlumbingRegex().IsMatch(line))
            {
                offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {trimmedLine.Trim()}");
            }

            braceDepth += CountBraceDelta(line);
            if (braceDepth <= 0)
            {
                insideTestMethod = false;
            }
        }

        return [.. offenses];
    }

    /// <summary>Finds serial collection definitions without a nearby SerialTestJustification attribute, returned as offenses.</summary>
    public static string[] FindUndocumentedSerialCollectionDefinitions(string filePath)
    {
        var repositoryRoot = GetRepositoryRoot();
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var trimmedLine = lines[lineIndex].TrimStart();
            if (!trimmedLine.StartsWith('[')
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
                    return candidate.StartsWith('[')
                        && candidate.Contains("SerialTestJustification", StringComparison.Ordinal);
                });

            if (!hasJustification)
            {
                offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
            }
        }

        return [.. offenses];
    }

    /// <summary>Returns the repository root: the nearest ancestor of the test output directory that holds ViajantesTurismo.slnx.</summary>
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

    /// <summary>Reports whether the path is build output, a feature-file code-behind, or other generated test source.</summary>
    public static bool IsGeneratedTestPath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal)
            || normalizedPath.Contains("/Snapshots/", StringComparison.Ordinal)
            || normalizedPath.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\b(?:new\s+ServiceCollection\s*\(|BuildServiceProvider\s*\(|CreateScope\s*\(|CreateAsyncScope\s*\()", RegexOptions.Compiled)]
    private static partial Regex RawServiceProviderPlumbingRegex();

    [GeneratedRegex(@"^public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+\w+\s*\(", RegexOptions.Compiled)]
    private static partial Regex TestMethodSignatureRegex();

    [GeneratedRegex(@"^\s*public\s+.*\b(?:IServiceProvider|IServiceScope|CreateScope|CreateAsyncScope|RunInScope)\b", RegexOptions.Compiled)]
    private static partial Regex PublicServiceProviderReachThroughRegex();

    [GeneratedRegex(@"const\s+string\s+\w+\s*=\s*(?:(?:global::)?SharedKernel\.Testing\.)?(?:TestTraitNames|SharedKernelTestTraitNames)\.\w+\s*;", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RedirectingCanonicalTraitNameRegex();

    [GeneratedRegex("\"{3,}", RegexOptions.Compiled)]
    private static partial Regex RawStringDelimiterRegex();

    [GeneratedRegex(@"CollectionDefinition.*DisableParallelization\s*=\s*true", RegexOptions.Compiled)]
    private static partial Regex SerialCollectionDefinitionRegex();

    private static bool IsTestAttributeLine(string trimmedLine)
    {
        return trimmedLine.StartsWith("[Fact", StringComparison.Ordinal)
            || trimmedLine.StartsWith("[Theory", StringComparison.Ordinal);
    }

    private static int CountBraceDelta(string line)
    {
        return line.Count(static character => character == '{') - line.Count(static character => character == '}');
    }

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
