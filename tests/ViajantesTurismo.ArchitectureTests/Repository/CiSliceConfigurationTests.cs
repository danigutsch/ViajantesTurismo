using System.Text.Json;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiSliceConfigurationTests
{
    [Fact]
    public void Test_slice_matrix_matches_the_configured_slices()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-ci.json")));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        // Act
        string[] slices = [.. configuration.RootElement.GetProperty("slices").EnumerateArray()
            .Select(static slice => slice.GetProperty("name").GetString().ShouldNotBeNull())];
        var detection = workflow.Split("\n  detect-changes:", StringSplitOptions.None)[1]
            .Split("\n  calculate-version:", StringSplitOptions.None)[0];
        string[] matrix = [.. workflow.Split("\n  test-slices:", StringSplitOptions.None)[1]
            .Split("\n  build-and-test:", StringSplitOptions.None)[0]
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("- id: ", StringComparison.Ordinal))
            .Select(static line => line["- id: ".Length..])];

        // Assert
        matrix.ShouldBe(slices);
        foreach (var slice in slices)
        {
            detection.ShouldContain($"      {slice}_required: ${{{{ steps.change-scope.outputs['{slice}_required'] }}}}", StringComparison.Ordinal);
        }
    }
}
