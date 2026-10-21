using System.Xml.Linq;
using System.Text.Json;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, TestTraits.ArchitectureScope)]
public static class SharedKernelConsumptionTests
{
    [Fact]
    public static void Application_consumes_shared_kernel_packages_without_owning_library_sources()
    {
        // Arrange
        var root = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx"));

        // Act
        var sourceReferences = solution.Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .SelectMany(path => XDocument.Load(Path.Combine(root, path)).Descendants("ProjectReference"))
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Where(reference => reference.Contains("SharedKernel", StringComparison.Ordinal))
            .ToArray();

        // Assert
        sourceReferences.ShouldBeEmpty();
        var libraryFolders = solution.Descendants("Folder")
            .Where(folder => ((string?)folder.Attribute("Name"))?.Contains("/SharedKernel", StringComparison.Ordinal) == true);
        libraryFolders.ShouldBeEmpty();
        Directory.Exists(Path.Combine(root, "src", "SharedKernel")).ShouldBeFalse();
        Directory.EnumerateDirectories(Path.Combine(root, "tests"), "SharedKernel.*").ShouldBeEmpty();
    }

    [Fact]
    public static void Application_build_configuration_does_not_own_library_packaging_rules()
    {
        // Arrange
        var root = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();

        // Act
        var libraryRules = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(Path.Combine(root, "src"), "Directory.Build.*")
            .Where(path => File.ReadAllText(path).Contains("StartsWith('SharedKernel.')", StringComparison.Ordinal))
            .ToArray();

        // Assert
        libraryRules.ShouldBeEmpty();
    }

    [Fact]
    public static void All_project_locks_and_library_tools_resolve_the_pinned_candidate()
    {
        // Arrange
        var root = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        using var pin = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-candidate.json")));
        var version = pin.RootElement.GetProperty("packageVersion").GetString();
        using var tools = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "dotnet-tools.json")));

        // Act
        var lockPaths = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(root, "*.csproj")
            .Select(project => Path.Combine(Path.GetDirectoryName(project).ShouldNotBeNull(), "packages.lock.json"))
            .ToArray();

        // Assert
        lockPaths.ShouldNotBeEmpty();
        foreach (var lockPath in lockPaths)
        {
            File.Exists(lockPath).ShouldBeTrue(lockPath);
            using var packages = JsonDocument.Parse(File.ReadAllText(lockPath));
            foreach (var dependency in packages.RootElement.GetProperty("dependencies").EnumerateObject()
                .SelectMany(framework => framework.Value.EnumerateObject())
                .Where(dependency => dependency.Name.StartsWith("PandaBear.SharedKernel.", StringComparison.Ordinal)))
            {
                dependency.Value.GetProperty("resolved").GetString().ShouldBe(version);
            }
        }
        foreach (var tool in tools.RootElement.GetProperty("tools").EnumerateObject()
            .Where(tool => tool.Name.StartsWith("pandabear.sharedkernel.", StringComparison.OrdinalIgnoreCase)))
        {
            tool.Value.GetProperty("version").GetString().ShouldBe(version);
        }
    }

}
