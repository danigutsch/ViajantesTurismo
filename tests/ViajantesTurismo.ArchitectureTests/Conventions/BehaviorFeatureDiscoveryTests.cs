using System.Xml.Linq;
using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, "regression")]
public sealed class BehaviorFeatureDiscoveryTests
{
    [Theory]
    [InlineData("ViajantesTurismo.Admin.BehaviorTests")]
    [InlineData("ViajantesTurismo.Catalog.BehaviorTests")]
    public void Feature_generation_uses_only_source_specs_and_preserves_runtime_copies(string projectName)
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), "tests", projectName, $"{projectName}.csproj");

        // Act
        var project = XDocument.Load(path);
        var featureContent = project.Descendants("Content")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => value?.EndsWith(".feature", StringComparison.Ordinal) == true)
            .ToArray();
        var generatorItems = project.Descendants("ReqnrollFeatureFile").ToArray();
        var removedGeneratorItems = generatorItems.Select(item => item.Attribute("Remove")?.Value).OfType<string>().ToArray();
        var includedGeneratorItems = generatorItems.Select(item => item.Attribute("Include")?.Value).OfType<string>().ToArray();
        var runtimeCopy = project.Descendants("None")
            .Single(item => item.Attribute("Update")?.Value == "specs\\**\\*.feature")
            .Element("CopyToOutputDirectory")?.Value;

        // Assert
        featureContent.ShouldBe(["specs/**/*.feature"]);
        removedGeneratorItems.ShouldBe(["@(ReqnrollFeatureFile)"]);
        includedGeneratorItems.ShouldBe(["specs/**/*.feature"]);
        runtimeCopy.ShouldBe("PreserveNewest");
    }
}
