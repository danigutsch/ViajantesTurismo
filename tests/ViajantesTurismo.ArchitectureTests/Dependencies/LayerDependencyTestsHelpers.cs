using System.Text.RegularExpressions;
using System.Xml.Linq;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ViajantesTurismo.ArchitectureTests.Dependencies;

internal static partial class LayerDependencyTestsHelpers
{
    private static readonly string[] OptionalSubmoduleSegmentNames =
    [
        "Analyzers",
        "AspNet",
        "AspNetCore",
        "Azure",
        "CloudEvents",
        "CodeFixes",
        "Dapper",
        "EntityFrameworkCore",
        "Grafana",
        "Hosting",
        "Npgsql",
        "Redis",
        "SourceGenerator",
        "Web"
    ];

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
                + $"{Path.GetFileNameWithoutExtension(filePath)} uses Core as a project-name segment")
            .ToArray();
    }

    public static string[] FindSharedKernelRuntimeReferencesToDescendantOptionalSubmodules(string repositoryRoot)
    {
        return SharedKernelSourceProjectFiles(repositoryRoot)
            .Where(IsPrimarySharedKernelProjectFile)
            .SelectMany(filePath => FindRuntimeReferencesToDescendantOptionalSubmodules(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindSharedKernelRuntimeReferencesToSameFamilyOptionalSiblingSubmodules(string repositoryRoot)
    {
        return SharedKernelSourceProjectFiles(repositoryRoot)
            .SelectMany(filePath => FindRuntimeReferencesToSameFamilyOptionalSiblingSubmodules(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindAbstractionProjectImplementationReferences(string repositoryRoot)
    {
        return SourceProjectFiles(repositoryRoot)
            .Where(IsAbstractionsProjectFile)
            .SelectMany(filePath => FindImplementationReferencesFromAbstractionProject(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindLayerProjectReferenceDirectionViolations(string repositoryRoot)
    {
        return SourceProjectFiles(repositoryRoot)
            .SelectMany(filePath => FindLayerProjectReferenceDirectionViolations(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindDomainContractProjectReferences(string repositoryRoot)
    {
        return SourceProjectFiles(repositoryRoot)
            .Where(filePath => IsDomainProjectName(Path.GetFileNameWithoutExtension(filePath)))
            .SelectMany(filePath => FindDomainContractProjectReferences(repositoryRoot, filePath))
            .ToArray();
    }

    public static string[] FindUnsplitProductContractProjects(string repositoryRoot)
    {
        return SourceProjectFiles(repositoryRoot)
            .Where(filePath => IsUnsplitProductContractProjectName(Path.GetFileNameWithoutExtension(filePath)))
            .Select(filePath =>
                $"{Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/')}: "
                + $"{Path.GetFileNameWithoutExtension(filePath)} must use .Contracts.Application, .Contracts.Http, or .Contracts.IntegrationEvents")
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
                    "families may be multi-segment",
                    "runtime-reference descendant optional submodules through project or package",
                    "Optional/provider/tool suffixes are not allowed",
                    "Optional submodules may reference the same-family primary module",
                    "Domain projects must not reference any `ViajantesTurismo.*.Contracts.*` project.",
                    "Product contract projects must use `.Contracts.Application`, `.Contracts.Http`, or `.Contracts.IntegrationEvents`.",
                    "Abstraction projects must not reference same-family implementation packages, provider adapters, persistence projects, web/API hosts, or adapter packages."
                ]),
            (
                Path.Combine("docs", "SHAREDKERNEL_PACKAGING.md"),
                [
                    "Do not create `SharedKernel.<Capability>.Core` packages.",
                    "families may be multi-segment",
                    "`Abstractions` modules are dependency-inversion surfaces, not implementation hosts.",
                    "runtime-reference descendant optional submodules through project or package",
                    "Optional/provider/tool suffixes are not allowed"
                ])
        ];
    }

    private static IEnumerable<string> MissingDocumentationRuleSnippets(
        string repositoryRoot,
        string documentPath,
        string[] snippets)
    {
        var absoluteDocumentPath = Path.Combine(repositoryRoot, documentPath);
        var normalizedDocumentPath = documentPath.Replace('\\', '/');

        if (!File.Exists(absoluteDocumentPath))
        {
            return [$"{normalizedDocumentPath}: document not found"];
        }

        var documentText = File.ReadAllText(absoluteDocumentPath);

        return snippets
            .Where(snippet => !documentText.Contains(snippet, StringComparison.Ordinal))
            .Select(snippet => $"{normalizedDocumentPath}: missing rule snippet: {snippet}");
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
            .Any(segment => segment.Equals("Core", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPrimarySharedKernelProjectFile(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return IsPrimarySharedKernelProjectName(projectName);
    }

    private static bool IsPrimarySharedKernelProjectName(string projectName)
    {
        return projectName.StartsWith("SharedKernel.", StringComparison.OrdinalIgnoreCase)
            && !IsAbstractionsProjectName(projectName)
            && !IsSharedKernelTestingSubmodule(projectName)
            && !projectName.Split('.')
                .Skip(2)
                .Any(segment => OptionalSubmoduleSegmentNames.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsSharedKernelTestingSubmodule(string projectName)
    {
        return projectName.StartsWith("SharedKernel.Testing.", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindRuntimeReferencesToDescendantOptionalSubmodules(
        string repositoryRoot,
        string filePath)
    {
        var referencingProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        var projectReferences = RuntimeProjectReferences(filePath)
            .Where(reference => IsRuntimeProjectReference(reference.Element))
            .Where(reference => IsDescendantSharedKernelProjectReference(
                referencingProjectName,
                reference.ReferencedProjectName))
            .Where(reference => !IsAbstractionsProjectName(reference.ReferencedProjectName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");

        var packageReferences = PackageReferences(filePath)
            .Where(reference => IsDescendantSharedKernelProjectReference(
                referencingProjectName,
                reference.PackageName))
            .Where(reference => !IsAbstractionsProjectName(reference.PackageName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.PackageName}: "
                + $"PackageReference Include=\"{reference.Include}\"");

        return projectReferences.Concat(packageReferences);
    }

    private static IEnumerable<string> FindRuntimeReferencesToSameFamilyOptionalSiblingSubmodules(
        string repositoryRoot,
        string filePath)
    {
        var referencingProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        var projectReferences = RuntimeProjectReferences(filePath)
            .Where(reference => IsRuntimeProjectReference(reference.Element))
            .Where(reference => IsSameFamilyOptionalSiblingSubmoduleReference(
                referencingProjectName,
                reference.ReferencedProjectName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");

        var packageReferences = PackageReferences(filePath)
            .Where(reference => IsSameFamilyOptionalSiblingSubmoduleReference(
                referencingProjectName,
                reference.PackageName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.PackageName}: "
                + $"PackageReference Include=\"{reference.Include}\"");

        return projectReferences.Concat(packageReferences);
    }

    private static IEnumerable<string> FindLayerProjectReferenceDirectionViolations(
        string repositoryRoot,
        string filePath)
    {
        var referencingProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        return RuntimeProjectReferences(filePath)
            .Where(reference => IsRuntimeProjectReference(reference.Element))
            .Where(reference => IsLayerProjectReferenceDirectionViolation(
                referencingProjectName,
                reference.ReferencedProjectName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");
    }

    private static IEnumerable<string> FindDomainContractProjectReferences(
        string repositoryRoot,
        string filePath)
    {
        var referencingProjectName = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        return RuntimeProjectReferences(filePath)
            .Where(reference => IsRuntimeProjectReference(reference.Element))
            .Where(reference => IsContractsProjectName(reference.ReferencedProjectName))
            .Select(reference =>
                $"{relativePath}: {referencingProjectName} -> {reference.ReferencedProjectName}: "
                + $"ProjectReference Include=\"{reference.Include}\"");
    }

    private static IEnumerable<(XElement Element, string Include, string ReferencedProjectName)> RuntimeProjectReferences(
        string filePath)
    {
        var document = XDocument.Load(filePath);

        foreach (var element in document.Descendants("ProjectReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            yield return (element, include, GetReferencedProjectName(filePath, include));
        }
    }

    private static IEnumerable<(XElement Element, string Include, string PackageName)> PackageReferences(string filePath)
    {
        var document = XDocument.Load(filePath);

        foreach (var element in document.Descendants("PackageReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            yield return (element, include, include);
        }
    }

    private static bool IsSameFamilyOptionalSiblingSubmoduleReference(
        string referencingProjectName,
        string referencedProjectName)
    {
        if (!IsSharedKernelProjectName(referencingProjectName)
            || !IsSharedKernelProjectName(referencedProjectName)
            || !IsOptionalSharedKernelSubmoduleProjectName(referencingProjectName)
            || !IsOptionalSharedKernelSubmoduleProjectName(referencedProjectName)
            || IsAbstractionsProjectName(referencedProjectName))
        {
            return false;
        }

        var referencingFamilyName = GetSharedKernelFamilyName(referencingProjectName);
        var referencedFamilyName = GetSharedKernelFamilyName(referencedProjectName);

        return referencingFamilyName.Equals(referencedFamilyName, StringComparison.OrdinalIgnoreCase)
            && !IsSameFamilyParentProjectReference(referencingFamilyName, referencingProjectName, referencedProjectName);
    }

    private static bool IsSameFamilyParentProjectReference(
        string familyName,
        string referencingProjectName,
        string referencedProjectName)
    {
        return referencedProjectName.Equals(familyName, StringComparison.OrdinalIgnoreCase)
            || (referencingProjectName.StartsWith($"{referencedProjectName}.", StringComparison.OrdinalIgnoreCase)
                && referencedProjectName.StartsWith($"{familyName}.", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOptionalSharedKernelSubmoduleProjectName(string projectName)
    {
        return !GetSharedKernelFamilyName(projectName).Equals(projectName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSharedKernelFamilyName(string projectName)
    {
        var segments = projectName.Split('.');
        for (var index = 2; index < segments.Length; index++)
        {
            if (OptionalSubmoduleSegmentNames.Contains(segments[index], StringComparer.OrdinalIgnoreCase))
            {
                return string.Join('.', segments.Take(index));
            }
        }

        return projectName;
    }

    private static bool IsLayerProjectReferenceDirectionViolation(
        string referencingProjectName,
        string referencedProjectName)
    {
        if (IsCatalogProjectName(referencingProjectName) && IsAdminProjectName(referencedProjectName))
        {
            return !IsContractsIntegrationEventsProjectName(referencedProjectName);
        }

        if (IsSharedKernelProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName);
        }

        if (IsContractsProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName)
                && !IsAllowedContractProjectReference(referencingProjectName, referencedProjectName);
        }

        if (IsDomainProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName);
        }

        if (IsApplicationProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName)
                && !IsSameContextLayerProjectName(referencingProjectName, referencedProjectName, "Domain")
                && !IsAllowedApplicationContractReference(referencingProjectName, referencedProjectName);
        }

        if (IsInfrastructureProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName)
                && !IsResourcesProjectName(referencedProjectName)
                && !IsSameContextLayerProjectName(referencingProjectName, referencedProjectName, "Application")
                && !IsSameContextLayerProjectName(referencingProjectName, referencedProjectName, "Domain");
        }

        if (IsApiProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName)
                && !IsResourcesProjectName(referencedProjectName)
                && !IsServiceDefaultsProjectName(referencedProjectName)
                && !IsSameContextLayerProjectName(referencingProjectName, referencedProjectName, "Application")
                && !IsAllowedApiContractReference(referencingProjectName, referencedProjectName)
                && !IsSameContextLayerProjectName(referencingProjectName, referencedProjectName, "Infrastructure");
        }

        if (IsWebProjectName(referencingProjectName))
        {
            return IsProductProjectName(referencedProjectName)
                && !IsContractsApplicationProjectName(referencedProjectName)
                && !IsContractsHttpProjectName(referencedProjectName)
                && !IsResourcesProjectName(referencedProjectName)
                && !IsServiceDefaultsProjectName(referencedProjectName);
        }

        return false;
    }

    private static bool IsAllowedContractProjectReference(string referencingProjectName, string referencedProjectName)
    {
        return IsContractsHttpProjectName(referencingProjectName)
            && IsSameBoundedContextProject(referencingProjectName, referencedProjectName)
            && IsContractsApplicationProjectName(referencedProjectName);
    }

    private static bool IsAllowedApplicationContractReference(string referencingProjectName, string referencedProjectName)
    {
        return IsContractsApplicationProjectName(referencedProjectName)
            ? IsSameBoundedContextProject(referencingProjectName, referencedProjectName)
            : IsContractsIntegrationEventsProjectName(referencedProjectName);
    }

    private static bool IsAllowedApiContractReference(string referencingProjectName, string referencedProjectName)
    {
        return IsSameBoundedContextProject(referencingProjectName, referencedProjectName)
            && (IsContractsApplicationProjectName(referencedProjectName)
                || IsContractsHttpProjectName(referencedProjectName));
    }

    private static bool IsProductProjectName(string projectName)
    {
        return projectName.StartsWith("ViajantesTurismo.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSharedKernelProjectName(string projectName)
    {
        return projectName.StartsWith("SharedKernel.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDomainProjectName(string projectName)
    {
        return projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApplicationProjectName(string projectName)
    {
        return projectName.EndsWith(".Application", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInfrastructureProjectName(string projectName)
    {
        return projectName.EndsWith(".Infrastructure", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApiProjectName(string projectName)
    {
        return projectName.EndsWith(".ApiService", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWebProjectName(string projectName)
    {
        return projectName.EndsWith(".Web", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractsProjectName(string projectName)
    {
        return IsProductProjectName(projectName)
            && HasProjectNameSegment(projectName, "Contracts");
    }

    private static bool IsContractsApplicationProjectName(string projectName)
    {
        return projectName.Contains(".Contracts.Application", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractsHttpProjectName(string projectName)
    {
        return projectName.Contains(".Contracts.Http", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractsIntegrationEventsProjectName(string projectName)
    {
        return projectName.Contains(".Contracts.IntegrationEvents", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsplitProductContractProjectName(string projectName)
    {
        return IsProductProjectName(projectName)
            && projectName.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResourcesProjectName(string projectName)
    {
        return projectName.Equals("ViajantesTurismo.Resources", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsServiceDefaultsProjectName(string projectName)
    {
        return projectName.Equals("ViajantesTurismo.ServiceDefaults", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCatalogProjectName(string projectName)
    {
        return projectName.StartsWith("ViajantesTurismo.Catalog.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdminProjectName(string projectName)
    {
        return projectName.StartsWith("ViajantesTurismo.Admin.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameContextLayerProjectName(
        string referencingProjectName,
        string referencedProjectName,
        string layerSegment)
    {
        return IsSameBoundedContextProject(referencingProjectName, referencedProjectName)
            && HasProjectNameSegment(referencedProjectName, layerSegment);
    }

    private static bool IsSameBoundedContextProject(string firstProjectName, string secondProjectName)
    {
        var firstContextName = GetBoundedContextName(firstProjectName);
        var secondContextName = GetBoundedContextName(secondProjectName);

        return firstContextName is not null
            && secondContextName is not null
            && firstContextName.Equals(secondContextName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetBoundedContextName(string projectName)
    {
        var segments = projectName.Split('.');
        if (segments.Length < 2 || !segments[0].Equals("ViajantesTurismo", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return segments[1] is "Admin" or "Catalog"
            ? segments[1]
            : null;
    }

    private static bool IsAbstractionsProjectFile(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return IsAbstractionsProjectName(projectName);
    }

    private static bool IsAbstractionsProjectName(string projectName) =>
        projectName.EndsWith(".Abstractions", StringComparison.OrdinalIgnoreCase);

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

        return !IsAbstractionsProjectName(referencedProjectName)
            && (referencedProjectName.Equals(abstractionFamilyName, StringComparison.OrdinalIgnoreCase)
                || referencedProjectName.StartsWith($"{abstractionFamilyName}.", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAbstractionFamilyName(string abstractionProjectName)
    {
        const string abstractionsSegment = ".Abstractions";
        var segmentIndex = abstractionProjectName.IndexOf(abstractionsSegment, StringComparison.OrdinalIgnoreCase);

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

        return segments.Any(segment => segmentNames.Contains(segment, StringComparer.OrdinalIgnoreCase));
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
        return referencedProjectName.StartsWith("SharedKernel.", StringComparison.OrdinalIgnoreCase)
            && referencedProjectName.StartsWith($"{referencingProjectName}.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasProjectNameSegment(string projectName, string segmentName)
    {
        return projectName.Split('.')
            .Any(segment => segment.Equals(segmentName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool NamesEntityFrameworkCoreAdapter(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return projectName.EndsWith(".EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            || projectName.Contains(".EntityFrameworkCore.", StringComparison.OrdinalIgnoreCase);
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

        return fileName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Application", StringComparison.OrdinalIgnoreCase)
            || IsContractsProjectName(fileName);
    }

    private static bool IsProviderNeutralSharedKernelProject(string filePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);

        return projectName.StartsWith("SharedKernel.", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".Npgsql", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".Dapper", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".Azure", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".Redis", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".CloudEvents", StringComparison.OrdinalIgnoreCase)
            && !projectName.Contains(".Aspire.Hosting", StringComparison.OrdinalIgnoreCase);
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
            .Any(packageName => packageName is not null && packageName.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAdapterPackage(string packageName)
    {
        return packageName.Equals("Npgsql", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Npgsql.", StringComparison.OrdinalIgnoreCase)
            || packageName.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            || packageName.Equals("Dapper", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Azure.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Aspire.Npgsql", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Aspire.StackExchange.Redis", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("StackExchange.Redis", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("RabbitMQ.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("MassTransit", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(?:(static\s+)?(global::)?ViajantesTurismo(\.|;)|[A-Za-z_][A-Za-z0-9_]*\s*=\s*(global::)?ViajantesTurismo(\.|;))")]
    private static partial Regex ProductUsingDirectiveRegex();
}
