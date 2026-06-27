using System.Text.RegularExpressions;
using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

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

    public static void AssertFileContains(string filePath, string expectedText)
    {
        var fileContents = File.ReadAllText(filePath);
        Assert.Contains(expectedText, fileContents, StringComparison.Ordinal);
    }

    public static void AssertFileDoesNotExist(string filePath)
    {
        Assert.False(File.Exists(filePath), $"Did not expect file to exist: {filePath}");
    }

    public static void AssertFileDoesNotContain(string filePath, Regex unexpectedPattern)
    {
        var fileContents = File.ReadAllText(filePath);
        Assert.DoesNotMatch(unexpectedPattern, fileContents);
    }

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

    public static string[] FindUndocumentedSerialTests(string filePath)
    {
        var fileContents = File.ReadAllText(filePath);
        if (!fileContents.Contains("AspireSerialSystemTestBase", StringComparison.Ordinal))
        {
            return [];
        }

        var repositoryRoot = GetRepositoryRoot();
        var offenses = new List<string>();
        foreach (Match match in SerialTestMethodRegex().Matches(fileContents))
        {
            var attributes = match.Groups["attributes"].Value;
            if (!TestAttributeRegex().IsMatch(attributes))
            {
                continue;
            }

            if (SerialReasonAttributeRegex().IsMatch(attributes))
            {
                continue;
            }

            offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}: {match.Groups["method"].Value}");
        }

        return [.. offenses];
    }

    public static string[] FindUndocumentedSerialCollectionDefinitions(string filePath)
    {
        var repositoryRoot = GetRepositoryRoot();
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var trimmedLine = lines[lineIndex].TrimStart();
            if (!trimmedLine.StartsWith('[')
                || !trimmedLine.Contains("CollectionDefinition", StringComparison.Ordinal)
                || !trimmedLine.Contains("DisableParallelization = true", StringComparison.Ordinal))
            {
                continue;
            }

            var hasJustification = lines
                .Skip(Math.Max(0, lineIndex - 3))
                .Take(3)
                .Any(line => line.Contains("SerialTestJustification(\"", StringComparison.Ordinal));

            if (!hasJustification)
            {
                offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
            }
        }

        return [.. offenses];
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

    public static bool IsGeneratedTestPath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal)
            || normalizedPath.Contains("/Snapshots/", StringComparison.Ordinal)
            || normalizedPath.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*protected\s+.*\bClearDatabase(?:Async)?\s*\(", RegexOptions.Compiled | RegexOptions.Multiline)]
    public static partial Regex ProtectedClearDatabaseMemberRegex();

    [GeneratedRegex(@"\b(?:new\s+ServiceCollection\s*\(|BuildServiceProvider\s*\(|CreateScope\s*\(|CreateAsyncScope\s*\()", RegexOptions.Compiled)]
    private static partial Regex RawServiceProviderPlumbingRegex();

    [GeneratedRegex(@"^public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+\w+\s*\(", RegexOptions.Compiled)]
    private static partial Regex TestMethodSignatureRegex();

    [GeneratedRegex(@"^\s*public\s+.*\b(?:IServiceProvider|IServiceScope|CreateScope|CreateAsyncScope|RunInScope)\b", RegexOptions.Compiled)]
    private static partial Regex PublicServiceProviderReachThroughRegex();

    [GeneratedRegex(@"(?<attributes>(?:\s*\[[^\]]+\]\s*)+)\s*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+(?<method>\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex SerialTestMethodRegex();

    [GeneratedRegex(@"\[(?:Fact|Theory)(?:\(|\])", RegexOptions.Compiled)]
    private static partial Regex TestAttributeRegex();

    [GeneratedRegex(@"\[SerialE2EReason\(\s*""[^""\r\n\s][^""\r\n]*""", RegexOptions.Compiled)]
    private static partial Regex SerialReasonAttributeRegex();

    [GeneratedRegex(@"const\s+string\s+\w+\s*=\s*(?:(?:global::)?SharedKernel\.Testing\.)?(?:TestTraitNames|SharedKernelTestTraitNames)\.\w+\s*;", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RedirectingCanonicalTraitNameRegex();

    [GeneratedRegex("\"{3,}", RegexOptions.Compiled)]
    private static partial Regex RawStringDelimiterRegex();

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
