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
        return SharedKernelSourceProjectFiles(repositoryRoot)
            .Where(ReferencesEntityFrameworkCorePackage)
            .Where(filePath => !NamesEntityFrameworkCoreAdapter(filePath))
            .Select(filePath => Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
    }

    public static string[] FindSharedKernelCoreSegmentProjectViolations(string repositoryRoot)
    {
        return SharedKernelSourceProjectFiles(repositoryRoot)
            .Where(HasCoreProjectNameSegment)
            .Select(filePath =>
                $"{Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/')}: "
                + $"{Path.GetFileNameWithoutExtension(filePath)} uses Core as a package-name segment")
            .ToArray();
    }

    public static string[] FindSharedKernelRuntimeReferencesToDescendantOptionalSubmodules(string repositoryRoot)
    {
        return SharedKernelSourceProjectFiles(repositoryRoot)
            .SelectMany(filePath => FindRuntimeReferencesToDescendantOptionalSubmodules(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindAbstractionProjectImplementationReferences(string repositoryRoot)
    {
        return SourceProjectFiles(repositoryRoot)
            .Where(HasAbstractionsProjectNameSegment)
            .SelectMany(filePath => FindImplementationReferencesFromAbstractionProject(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindModuleBoundaryDocumentationRuleGaps(string repositoryRoot)
    {
        return RequiredModuleBoundaryDocumentationSnippets()
            .SelectMany(rule => MissingDocumentationRuleSnippets(repositoryRoot, rule.DocumentPath, rule.Snippets))
            .ToArray();
    }

    private static (string DocumentPath, string[] Snippets)[] RequiredModuleBoundaryDocumentationSnippets()
    {
        return
        [
            (
                Path.Combine("docs", "architecture", "boundaries-and-dependencies.md"),
                [
                    "`SharedKernel.<Capability>` is the primary module and core surface for that capability.",
                    "Primary modules must not runtime-reference descendant optional submodules.",
                    "reference the primary module, a nearer parent module, or an explicit `Abstractions` module.",
                    "Abstraction projects must not reference same-family implementation packages, provider adapters, persistence projects, web/API hosts, or adapter packages."
                ]),
            (
                Path.Combine("docs", "SHAREDKERNEL_PACKAGING.md"),
                [
                    "Do not create `SharedKernel.<Capability>.Core` packages.",
                    "`Abstractions` modules are dependency-inversion surfaces, not implementation hosts.",
                    "Primary modules must not runtime-reference descendant optional submodules."
                ])
        ];
    }

    private static IEnumerable<string> MissingDocumentationRuleSnippets(
        string repositoryRoot,
        string documentPath,
        string[] snippets)
    {
        var absoluteDocumentPath = Path.Combine(repositoryRoot, documentPath);
        var documentText = File.ReadAllText(absoluteDocumentPath);

        return snippets
            .Where(snippet => !documentText.Contains(snippet, StringComparison.Ordinal))
            .Select(snippet => $"{documentPath}: missing rule snippet: {snippet}");
    }

    private static IEnumerable<string> SourceProjectFiles(string repositoryRoot)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        return Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(IsSourceFile);
    }

    private static IEnumerable<string> SharedKernelSourceProjectFiles(string repositoryRoot)
    {
        var sharedKernelRoot = Path.Combine(repositoryRoot, "src", "SharedKernel");

        return Directory.EnumerateFiles(sharedKernelRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(IsSourceFile);
    }

    private static bool HasCoreProjectNameSegment(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return projectName.Split('.')
            .Any(segment => segment.Equals("Core", StringComparison.Ordinal));
    }

    private static IEnumerable<string> FindRuntimeReferencesToDescendantOptionalSubmodules(
        string repositoryRoot,
        string filePath)
    {
        var referencingProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');
        var document = XDocument.Load(filePath);

        return document.Descendants("ProjectReference")
            .Select(element => (Element: element, Include: element.Attribute("Include")?.Value))
            .Where(reference => !string.IsNullOrWhiteSpace(reference.Include))
            .Select(reference => (
                reference.Element,
                reference.Include,
                ReferencedProjectName: GetReferencedProjectName(filePath, reference.Include)))
            .Where(reference => IsRuntimeProjectReference(reference.Element))
            .Where(reference => IsDescendantSharedKernelProjectReference(
                referencingProjectName,
                reference.ReferencedProjectName))
            .Where(reference => !HasProjectNameSegment(reference.ReferencedProjectName, "Abstractions"))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");
    }

    private static bool HasAbstractionsProjectNameSegment(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return HasProjectNameSegment(projectName, "Abstractions");
    }

    private static IEnumerable<string> FindImplementationReferencesFromAbstractionProject(
        string repositoryRoot,
        string filePath)
    {
        var abstractionProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');
        var document = XDocument.Load(filePath);

        var projectReferences = document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => (Include: include, ReferencedProjectName: GetReferencedProjectName(filePath, include)))
            .Where(reference => IsImplementationProjectReference(abstractionProjectName, reference.ReferencedProjectName))
            .Select(reference =>
                $"{relativePath}: {abstractionProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");

        var packageReferences = document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(packageName => packageName is not null && IsImplementationPackageReference(abstractionProjectName, packageName))
            .Select(packageName => $"{relativePath}: PackageReference Include=\"{packageName}\"");

        return projectReferences.Concat(packageReferences);
    }

    private static bool IsImplementationPackageReference(string abstractionProjectName, string packageName)
    {
        return IsImplementationProjectReference(abstractionProjectName, packageName)
            || IsAdapterPackage(packageName);
    }

    private static bool IsImplementationProjectReference(string abstractionProjectName, string referencedProjectName)
    {
        return IsSameFamilyImplementationProjectReference(abstractionProjectName, referencedProjectName)
            || IsLayerDirectionViolation(abstractionProjectName, referencedProjectName)
            || HasImplementationProjectNameSegment(referencedProjectName);
    }

    private static bool IsLayerDirectionViolation(string abstractionProjectName, string referencedProjectName)
    {
        return HasProjectNameSegment(abstractionProjectName, "Domain")
            ? HasAnyProjectNameSegment(referencedProjectName, ["Application", "Infrastructure", "ApiService", "Web"])
            : HasProjectNameSegment(abstractionProjectName, "Application")
                && HasAnyProjectNameSegment(referencedProjectName, ["Infrastructure", "ApiService", "Web"]);
    }

    private static bool IsSameFamilyImplementationProjectReference(string abstractionProjectName, string referencedProjectName)
    {
        var abstractionFamilyName = GetAbstractionFamilyName(abstractionProjectName);

        return !HasProjectNameSegment(referencedProjectName, "Abstractions")
            && (referencedProjectName.Equals(abstractionFamilyName, StringComparison.Ordinal)
                || referencedProjectName.StartsWith($"{abstractionFamilyName}.", StringComparison.Ordinal));
    }

    private static string GetAbstractionFamilyName(string abstractionProjectName)
    {
        const string abstractionsSegment = ".Abstractions";
        var segmentIndex = abstractionProjectName.IndexOf(abstractionsSegment, StringComparison.Ordinal);

        return segmentIndex >= 0
            ? abstractionProjectName[..segmentIndex]
            : abstractionProjectName;
    }

    private static bool HasImplementationProjectNameSegment(string projectName)
    {
        return HasAnyProjectNameSegment(
            projectName,
            [
                "Analyzers",
                "ApiService",
                "AspNet",
                "AspNetCore",
                "CloudEvents",
                "CodeFixes",
                "Dapper",
                "EntityFrameworkCore",
                "Hosting",
                "Infrastructure",
                "Npgsql",
                "Persistence",
                "SourceGenerator",
                "Web"
            ]);
    }

    private static bool HasAnyProjectNameSegment(string projectName, string[] segmentNames)
    {
        var segments = projectName.Split('.');

        return segments.Any(segment => segmentNames.Contains(segment, StringComparer.Ordinal));
    }

    private static string GetReferencedProjectName(string referencingProjectPath, string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            throw new InvalidOperationException($"ProjectReference in {referencingProjectPath} is missing Include.");
        }

        var projectDirectory = Path.GetDirectoryName(referencingProjectPath) ?? throw new InvalidOperationException($"Project path has no directory: {referencingProjectPath}");
        var referencedProjectPath = Path.GetFullPath(Path.Combine(projectDirectory, include));

        return Path.GetFileNameWithoutExtension(referencedProjectPath);
    }

    private static bool IsRuntimeProjectReference(XElement element)
    {
        return !HasAttributeValue(element, "ReferenceOutputAssembly", "false")
            && !HasAnalyzerOrPackagingOutputItemType(element);
    }

    private static bool HasAttributeValue(XElement element, string attributeName, string value)
    {
        return element.Attribute(attributeName)?.Value.Equals(value, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool HasAnalyzerOrPackagingOutputItemType(XElement element)
    {
        var outputItemType = element.Attribute("OutputItemType")?.Value;

        return outputItemType is not null
            && (outputItemType.Contains("Analyzer", StringComparison.OrdinalIgnoreCase)
                || outputItemType.Contains("Pack", StringComparison.OrdinalIgnoreCase)
                || outputItemType.Contains("Package", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDescendantSharedKernelProjectReference(
        string referencingProjectName,
        string referencedProjectName)
    {
        return referencedProjectName.StartsWith("SharedKernel.", StringComparison.Ordinal)
            && referencedProjectName.StartsWith($"{referencingProjectName}.", StringComparison.Ordinal);
    }

    private static bool HasProjectNameSegment(string projectName, string segmentName)
    {
        return projectName.Split('.')
            .Any(segment => segment.Equals(segmentName, StringComparison.Ordinal));
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
