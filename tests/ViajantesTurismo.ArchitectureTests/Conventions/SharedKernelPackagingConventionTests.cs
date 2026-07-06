namespace ViajantesTurismo.ArchitectureTests.Conventions;

public static class SharedKernelPackagingConventionTests
{
    [Fact]
    public static void SharedKernel_source_projects_follow_package_metadata_conventions()
    {
        // Arrange
        var repositoryRoot = SharedKernelPackagingConventionTestFiles.GetRepositoryRoot();
        var projectFiles = Directory.GetFiles(
            Path.Combine(repositoryRoot, "src", "SharedKernel"),
            "*.csproj",
            SearchOption.AllDirectories);

        // Act
        var violations = projectFiles
            .SelectMany(SharedKernelPackagingConventionTestFiles.GetPackageMetadataViolations)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        violations.ShouldBe([]);
    }

    [Fact]
    public static void SharedKernel_change_detection_covers_current_package_projects()
    {
        // Arrange
        var repositoryRoot = SharedKernelPackagingConventionTestFiles.GetRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "detect-changes.sh"));
        var activePatterns = SharedKernelPackagingConventionTestFiles.GetActiveQuotedEntries(script);
        var packageDirectories = Directory.GetDirectories(Path.Combine(repositoryRoot, "src", "SharedKernel"))
            .Select(directory => Path.GetRelativePath(repositoryRoot, directory).Replace('\\', '/') + "/**")
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Act
        var missingPatterns = packageDirectories
            .Where(pattern => !activePatterns.Contains(pattern))
            .ToArray();

        // Assert
        missingPatterns.ShouldBe([]);
    }

    [Fact]
    public static void Release_prep_workflow_covers_packaging_inputs()
    {
        // Arrange
        var repositoryRoot = SharedKernelPackagingConventionTestFiles.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", "release-prep.yml"));
        var pathEntries = SharedKernelPackagingConventionTestFiles.GetYamlPathEntries(workflow);

        string[] expectedPaths =
        [
            "docs/SHAREDKERNEL_PACKAGING.md",
            "src/Directory.Build.props",
            "src/Directory.Build.targets",
            "tools/SharedKernel.Versioning.Tool/**",
            "tools/SharedKernel.Testing.CodeFixRunner/**",
        ];

        // Act
        var missingPaths = expectedPaths
            .Where(path => !pathEntries.Contains(path))
            .ToArray();

        // Assert
        missingPaths.ShouldBe([]);
    }

    [Fact]
    public static void Samples_benchmarks_and_repository_only_helpers_are_non_packable()
    {
        // Arrange
        var repositoryRoot = SharedKernelPackagingConventionTestFiles.GetRepositoryRoot();
        string[] projectPaths =
        [
            "benchmarks/SharedKernel.Functional.Benchmarks/SharedKernel.Functional.Benchmarks.csproj",
            "benchmarks/SharedKernel.Mediator.Benchmarks/SharedKernel.Mediator.Benchmarks.csproj",
            "samples/Mediator/Mediator.Sample/Mediator.Sample.csproj",
            "samples/Results/BasicResults.Sample/BasicResults.Sample.csproj",
            "tests/SharedKernel.AspNetCoreTesting/SharedKernel.AspNetCoreTesting.csproj",
            "tests/SharedKernel.CodeFixes.Testing/SharedKernel.CodeFixes.Testing.csproj",
            "tests/SharedKernel.Mediator.Testing.ReferenceDispatcher/SharedKernel.Mediator.Testing.ReferenceDispatcher.csproj",
            "tests/SharedKernel.Testing.Contracts/SharedKernel.Testing.Contracts.csproj",
            "tests/SharedKernel.Testing.Integration/SharedKernel.Testing.Integration.csproj",
            "tests/SharedKernel.Testing.Packaging/SharedKernel.Testing.Packaging.csproj",
            "tests/SharedKernel.Testing.Scenarios/SharedKernel.Testing.Scenarios.csproj",
            "tests/SharedKernel.Testing.System/SharedKernel.Testing.System.csproj",
        ];

        // Act
        var packableProjects = projectPaths
            .Where(path => !string.Equals(
                SharedKernelPackagingConventionTestFiles.GetProperty(Path.Combine(repositoryRoot, path), "IsPackable"),
                "false",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Assert
        packableProjects.ShouldBe([]);
    }
}
