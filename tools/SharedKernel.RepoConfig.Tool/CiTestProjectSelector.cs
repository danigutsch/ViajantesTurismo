using System.Xml.Linq;

namespace SharedKernel.RepoConfig.Tool;

internal static class CiTestProjectSelector
{
    private const string SolutionPath = "ViajantesTurismo.slnx";
    private const string SliceDirectory = "scripts/ci-test-slices";
    private const string ArchitectureTests = "tests/ViajantesTurismo.ArchitectureTests/ViajantesTurismo.ArchitectureTests.csproj";
    private const string MediatorPackageConsumptionTests = "tests/SharedKernel.Mediator.PackageConsumptionTests/SharedKernel.Mediator.PackageConsumptionTests.csproj";
    private const string OpenApiToolTests = "tests/ViajantesTurismo.OpenApi.Tool.Tests/ViajantesTurismo.OpenApi.Tool.Tests.csproj";

    private static readonly HashSet<string> CanonicalSliceNames = new(StringComparer.Ordinal)
    {
        "admin-api-integration",
        "admin-integration",
        "admin-system",
        "fast-validation-1",
        "fast-validation-2",
        "mediator-heavy"
    };

    private static readonly string[] MediatorPackedProjects =
    [
        "src/SharedKernel/SharedKernel.Mediator.Abstractions/SharedKernel.Mediator.Abstractions.csproj",
        "src/SharedKernel/SharedKernel.Mediator/SharedKernel.Mediator.csproj",
        "src/SharedKernel/SharedKernel.Mediator.SourceGenerator/SharedKernel.Mediator.SourceGenerator.csproj",
        "src/SharedKernel/SharedKernel.Mediator.Analyzers/SharedKernel.Mediator.Analyzers.csproj",
        "src/SharedKernel/SharedKernel.Mediator.CodeFixes/SharedKernel.Mediator.CodeFixes.csproj",
        "src/SharedKernel/SharedKernel.Results/SharedKernel.Results.csproj",
        "src/SharedKernel/SharedKernel.BuildingBlocks/SharedKernel.BuildingBlocks.csproj",
        "src/SharedKernel/SharedKernel.Domain/SharedKernel.Domain.csproj",
        "src/SharedKernel/SharedKernel.Messaging/SharedKernel.Messaging.csproj",
        "src/SharedKernel/SharedKernel.Messaging.IntegrationEvents/SharedKernel.Messaging.IntegrationEvents.csproj",
        "src/SharedKernel/SharedKernel.Messaging.IntegrationEvents.SourceGenerator/SharedKernel.Messaging.IntegrationEvents.SourceGenerator.csproj"
    ];

    internal static readonly IReadOnlyList<string> OpenApiWindowsProjects = Array.AsReadOnly(
    [
        OpenApiToolTests,
        "src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj",
        "src/ViajantesTurismo.Branding.ApiService/ViajantesTurismo.Branding.ApiService.csproj",
        "src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj"
    ]);

    private static readonly HashSet<string> FullValidationFiles = new(StringComparer.Ordinal)
    {
        ".config/dotnet-tools.json",
        ".editorconfig",
        ".gitattributes",
        ".markdownlint-cli2.jsonc",
        ".markdownlint.json",
        "coverage.settings.xml",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "global.json",
        "NuGet.Config",
        "opencode.json",
        "tests/Directory.Build.props",
        "tests/xunit.runner.json",
        SolutionPath
    };

    private static readonly string[] FullValidationPrefixes =
    [
        ".github/actions/",
        ".github/workflows/",
        "scripts/ci-test-slices/",
        "tools/SharedKernel.RepoConfig.Tool/",
        "src/SharedKernel/SharedKernel.Style.Analyzers/",
        "src/SharedKernel/SharedKernel.Style.CodeFixes/",
        "src/SharedKernel/SharedKernel.Testing.Analyzers/"
    ];

    private static readonly HashSet<string> FullValidationScripts = new(StringComparer.Ordinal)
    {
        "scripts/collect-ci-build-test-diagnostics.sh",
        "scripts/collect-test-coverage.sh",
        "scripts/generate-sonar-coverage-report.sh",
        "scripts/install-playwright.sh",
        "scripts/refresh-dependency-lockfiles.sh",
        "scripts/run-ci-test-slice.sh",
        "scripts/run-sonar-analysis.sh",
        "scripts/run-tests-with-coverage.sh",
        "scripts/validate-sonar-analysis-config.sh",
        "scripts/write-ci-artifact-summary.sh",
        "scripts/write-github-sonar-summary.sh"
    };

    private static readonly HashSet<string> LowRiskFiles = new(StringComparer.Ordinal)
    {
        "scripts/commitlint.sh",
        "scripts/format-powershell-file.ps1",
        "scripts/lint-all.sh",
        "scripts/lint-gherkin.sh",
        "scripts/lint-gherkin.py",
        "scripts/lint-json.sh",
        "scripts/lint-json.py",
        "scripts/lint-markdown.sh",
        "scripts/validate-commit-message.sh",
        "setup-dev.ps1",
        "setup-dev.sh"
    };

    public static CiTestProjectSelection Select(
        string rootPath,
        IReadOnlyCollection<string> changedPaths,
        bool fullValidation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(changedPaths);

        var normalizedRoot = Path.GetFullPath(rootPath);
        var slices = LoadSlices(normalizedRoot);
        if (fullValidation)
        {
            return CreateFullSelection(slices, fallback: false);
        }

        var relevantPaths = changedPaths
            .Select(RepoConfigPaths.Normalize)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Where(static path => !IsDocumentationOrLowRisk(path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (relevantPaths.Length == 0)
        {
            return CreateEmptySelection(slices);
        }

        if (relevantPaths.Any(RequiresFullValidation))
        {
            return CreateFullSelection(slices, fallback: false);
        }

        try
        {
            var references = LoadProjectReferences(normalizedRoot);
            var projectDirectories = references.Keys
                .ToDictionary(
                    static path => path,
                    static path => RepoConfigPaths.Normalize(Path.GetDirectoryName(path) ?? string.Empty),
                    StringComparer.Ordinal);
            HashSet<string> changedProjects = new(StringComparer.Ordinal);

            foreach (var path in relevantPaths)
            {
                var owner = FindOwningProject(path, projectDirectories);
                if (owner is null)
                {
                    return CreateFullSelection(slices, fallback: true);
                }

                _ = changedProjects.Add(owner);
            }

            HashSet<string> selectedProjects = new(StringComparer.Ordinal);
            HashSet<string> coveredChangedProjects = new(StringComparer.Ordinal);
            var manifestProjects = slices.Values.SelectMany(static projects => projects).ToHashSet(StringComparer.Ordinal);
            foreach (var testProject in manifestProjects)
            {
                var dependencies = GetDependencyClosure(testProject, references);
                var coveredByTest = changedProjects.Where(dependencies.Contains).ToArray();
                if (coveredByTest.Length > 0)
                {
                    _ = selectedProjects.Add(testProject);
                    coveredChangedProjects.UnionWith(coveredByTest);
                }
            }

            var openApiChangedProjects = GetOpenApiChangedProjects(changedProjects, references);
            coveredChangedProjects.UnionWith(openApiChangedProjects);
            if (!changedProjects.IsSubsetOf(coveredChangedProjects))
            {
                return CreateFullSelection(slices, fallback: true);
            }

            var openApiRequired = openApiChangedProjects.Count > 0
                || selectedProjects.Contains(OpenApiToolTests);
            if (openApiRequired && manifestProjects.Contains(OpenApiToolTests))
            {
                _ = selectedProjects.Add(OpenApiToolTests);
            }

            _ = selectedProjects.Add(ArchitectureTests);

            var selectedBySlice = slices.ToDictionary(
                static pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.Where(selectedProjects.Contains).ToArray(),
                StringComparer.Ordinal);

            return new CiTestProjectSelection(
                BuildRequired: true,
                FallbackToFullValidation: false,
                OpenApiToolWindowsRequired: openApiRequired,
                SelectedProjectsBySlice: selectedBySlice);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or System.Xml.XmlException)
        {
            return CreateFullSelection(slices, fallback: true);
        }
    }

    internal static void ValidateCanonicalSliceManifests(string rootPath)
    {
        var normalizedRoot = Path.GetFullPath(rootPath);
        var slices = LoadSlices(normalizedRoot);
        var actualNames = slices.Keys.ToHashSet(StringComparer.Ordinal);
        if (!actualNames.SetEquals(CanonicalSliceNames))
        {
            var missing = CanonicalSliceNames.Except(actualNames).Order(StringComparer.Ordinal);
            var extra = actualNames.Except(CanonicalSliceNames).Order(StringComparer.Ordinal);
            throw new InvalidOperationException(
                $"CI test slice manifests must match the fixed workflow matrix. Missing: [{string.Join(", ", missing)}]. Extra: [{string.Join(", ", extra)}].");
        }

        var emptySlices = slices
            .Where(static slice => slice.Value.Length == 0)
            .Select(static slice => slice.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (emptySlices.Length > 0)
        {
            throw new InvalidOperationException(
                $"CI test slice manifests cannot be empty: [{string.Join(", ", emptySlices)}].");
        }
    }

    internal static void ValidateCanonicalSliceMembership(string rootPath)
    {
        var normalizedRoot = Path.GetFullPath(rootPath);
        var slices = LoadSlices(normalizedRoot);

        var solution = XDocument.Load(Path.Combine(normalizedRoot, SolutionPath));
        var expectedProjects = solution
            .Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Select(RepoConfigPaths.Normalize)
            .Where(static path => path.StartsWith("tests/", StringComparison.Ordinal))
            .Where(path => XDocument.Load(Path.Combine(normalizedRoot, path))
                .Descendants()
                .Where(static element => element.Name.LocalName == "PackageReference")
                .Any(static package => string.Equals(
                    package.Attribute("Include")?.Value,
                    "xunit.v3.mtp-v2",
                    StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);
        var assignments = slices
            .SelectMany(static slice => slice.Value.Select(project => (Slice: slice.Key, Project: project)))
            .ToArray();
        var assignedProjects = assignments
            .Select(static assignment => assignment.Project)
            .ToHashSet(StringComparer.Ordinal);
        var missingProjects = expectedProjects
            .Except(assignedProjects)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var invalidProjects = assignedProjects
            .Except(expectedProjects)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var duplicateProjects = assignments
            .GroupBy(static assignment => assignment.Project, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(group => $"{group.Key} ({string.Join(", ", group.Select(static assignment => assignment.Slice).Order(StringComparer.Ordinal))})")
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (missingProjects.Length > 0 || invalidProjects.Length > 0 || duplicateProjects.Length > 0)
        {
            throw new InvalidOperationException(
                $"CI test slice membership must assign every solution xUnit project exactly once. Missing: [{string.Join(", ", missingProjects)}]. Invalid: [{string.Join(", ", invalidProjects)}]. Duplicates: [{string.Join(", ", duplicateProjects)}].");
        }
    }

    private static Dictionary<string, string[]> LoadSlices(string rootPath)
    {
        var directory = Path.Combine(rootPath, SliceDirectory);
        return Directory.EnumerateFiles(directory, "*.txt")
            .Order(StringComparer.Ordinal)
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(path),
                path => File.ReadAllLines(path)
                    .Select(RepoConfigPaths.Normalize)
                    .Where(static project => !string.IsNullOrWhiteSpace(project))
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static Dictionary<string, string[]> LoadProjectReferences(string rootPath)
    {
        var solution = XDocument.Load(Path.Combine(rootPath, SolutionPath));
        var projectPaths = solution
            .Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Select(RepoConfigPaths.Normalize)
            .ToHashSet(StringComparer.Ordinal);

        Dictionary<string, string[]> references = new(StringComparer.Ordinal);
        foreach (var projectPath in projectPaths)
        {
            var projectFullPath = Path.Combine(rootPath, projectPath);
            var projectDirectory = Path.GetDirectoryName(projectFullPath)
                ?? throw new InvalidOperationException($"Project path has no directory: {projectPath}");
            var project = XDocument.Load(projectFullPath);
            var resolvedReferences = project
                .Descendants()
                .Where(static element => element.Name.LocalName == "ProjectReference")
                .Select(static element => element.Attribute("Include")?.Value)
                .OfType<string>()
                .Select(static reference => reference
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar))
                .Select(reference => Path.GetFullPath(reference, projectDirectory))
                .Select(path => RepoConfigPaths.RelativeTo(rootPath, path))
                .ToArray();

            if (resolvedReferences.Any(reference => !projectPaths.Contains(reference)))
            {
                throw new InvalidOperationException($"Project '{projectPath}' has an unresolved project reference.");
            }

            references.Add(projectPath, resolvedReferences);
        }

        if (references.TryGetValue(MediatorPackageConsumptionTests, out var mediatorReferences))
        {
            references[MediatorPackageConsumptionTests] = mediatorReferences
                .Concat(MediatorPackedProjects.Where(references.ContainsKey))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        return references;
    }

    private static string? FindOwningProject(
        string changedPath,
        Dictionary<string, string> projectDirectories)
    {
        return projectDirectories
            .Where(pair => string.Equals(changedPath, pair.Key, StringComparison.Ordinal)
                || (pair.Value.Length > 0 && changedPath.StartsWith($"{pair.Value}/", StringComparison.Ordinal)))
            .OrderByDescending(static pair => pair.Value.Length)
            .Select(static pair => pair.Key)
            .FirstOrDefault();
    }

    private static HashSet<string> GetDependencyClosure(
        string projectPath,
        Dictionary<string, string[]> references)
    {
        HashSet<string> closure = new(StringComparer.Ordinal);
        Stack<string> pending = new();
        pending.Push(projectPath);

        while (pending.TryPop(out var current))
        {
            if (!closure.Add(current))
            {
                continue;
            }

            if (!references.TryGetValue(current, out var dependencies))
            {
                throw new InvalidOperationException($"Project is not in the solution graph: {current}");
            }

            foreach (var dependency in dependencies)
            {
                pending.Push(dependency);
            }
        }

        return closure;
    }

    private static HashSet<string> GetOpenApiChangedProjects(
        HashSet<string> changedProjects,
        Dictionary<string, string[]> references)
    {
        HashSet<string> affectedProjects = new(StringComparer.Ordinal);
        foreach (var project in OpenApiWindowsProjects.Skip(1))
        {
            if (!references.ContainsKey(project))
            {
                continue;
            }

            affectedProjects.UnionWith(changedProjects.Where(GetDependencyClosure(project, references).Contains));
        }

        return affectedProjects;
    }

    private static bool IsDocumentationOrLowRisk(string path) =>
        path.StartsWith("docs/", StringComparison.Ordinal)
        || path is "README.md" or "CONTRIBUTING.md"
        || LowRiskFiles.Contains(path);

    private static bool RequiresFullValidation(string path) =>
        FullValidationFiles.Contains(path)
        || FullValidationScripts.Contains(path)
        || FullValidationPrefixes.Any(path.StartsWith)
        || path.EndsWith(".props", StringComparison.Ordinal)
        || path.EndsWith(".targets", StringComparison.Ordinal)
        || path.EndsWith(".slnx", StringComparison.Ordinal);

    private static CiTestProjectSelection CreateFullSelection(
        Dictionary<string, string[]> slices,
        bool fallback) =>
        new(
            BuildRequired: true,
            FallbackToFullValidation: fallback,
            OpenApiToolWindowsRequired: true,
            SelectedProjectsBySlice: slices.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.Ordinal));

    private static CiTestProjectSelection CreateEmptySelection(Dictionary<string, string[]> slices) =>
        new(
            BuildRequired: false,
            FallbackToFullValidation: false,
            OpenApiToolWindowsRequired: false,
            SelectedProjectsBySlice: slices.ToDictionary(
                static pair => pair.Key,
                static _ => (IReadOnlyList<string>)Array.Empty<string>(),
                StringComparer.Ordinal));
}
