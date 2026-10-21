using System.Xml.Linq;

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
        Directory.Exists(Path.Combine(root, "src", "SharedKernel")).ShouldBeFalse();
        Directory.EnumerateDirectories(Path.Combine(root, "tests"), "SharedKernel.*").ShouldBeEmpty();
    }
}
