using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class SharedKernelPackagingConventionTestFiles
{
    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    public static IEnumerable<string> GetPackageMetadataViolations(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project path has no directory.");
        var packageId = GetProperty(document, "PackageId");
        if (packageId != Path.GetFileNameWithoutExtension(projectPath))
        {
            yield return $"{projectPath}: PackageId must match the project file name.";
        }

        if (string.IsNullOrWhiteSpace(GetProperty(document, "PackageTags")))
        {
            yield return $"{projectPath}: PackageTags is required.";
        }

        if (File.Exists(Path.Combine(projectDirectory, "README.md")))
        {
            if (GetProperty(document, "PackageReadmeFile") != "README.md")
            {
                yield return $"{projectPath}: PackageReadmeFile must include README.md.";
            }

            if (!PacksReadme(document))
            {
                yield return $"{projectPath}: README.md must be packed.";
            }
        }

        var targetFramework = GetProperty(document, "TargetFramework");
        if (string.Equals(targetFramework, "netstandard2.0", StringComparison.OrdinalIgnoreCase)
            && IsRoslynPackage(packageId)
            && !string.Equals(GetProperty(document, "IsAotCompatible"), "false", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{projectPath}: netstandard2.0 Roslyn packages must opt out of AOT compatibility.";
        }
    }

    public static string GetProperty(string projectPath, string propertyName)
    {
        var document = XDocument.Load(projectPath);
        return GetProperty(document, propertyName);
    }

    public static HashSet<string> GetActiveQuotedEntries(string content)
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in content.Split(Environment.NewLine))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            AddQuotedEntry(entries, trimmed, '"');
            AddQuotedEntry(entries, trimmed, '\'');
        }

        return entries;
    }

    public static HashSet<string> GetYamlPathEntries(string content)
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in content.Split(Environment.NewLine))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }

            entries.Add(trimmed[2..].Trim());
        }

        return entries;
    }

    private static string GetProperty(XDocument document, string propertyName)
    {
        return document.Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))
            ?.Value.Trim() ?? string.Empty;
    }

    private static bool PacksReadme(XDocument document)
    {
        return document.Descendants().Any(element =>
            element.Name.LocalName == "None"
            && element.Attribute("Include")?.Value == "README.md"
            && string.Equals(element.Attribute("Pack")?.Value, "true", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddQuotedEntry(HashSet<string> entries, string line, char quote)
    {
        var quoteText = quote.ToString();
        var start = line.IndexOf(quoteText, StringComparison.Ordinal);
        if (start < 0)
        {
            return;
        }

        var end = line.IndexOf(quoteText, start + 1, StringComparison.Ordinal);
        if (end > start)
        {
            entries.Add(line[(start + 1)..end]);
        }
    }

    private static bool IsRoslynPackage(string packageId) =>
        packageId.EndsWith(".Analyzers", StringComparison.Ordinal)
        || packageId.EndsWith(".CodeFixes", StringComparison.Ordinal)
        || packageId.EndsWith(".SourceGenerator", StringComparison.Ordinal);
}
