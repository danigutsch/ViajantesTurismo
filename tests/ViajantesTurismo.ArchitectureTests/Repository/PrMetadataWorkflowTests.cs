using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class PrMetadataWorkflowTests
{
    [Fact]
    public async Task Pr_metadata_calls_the_shared_workflow_with_the_pinned_ci_tool_source()
    {
        // Arrange
        var path = Path.Combine(RepositoryTestProcess.GetRepositoryRoot(), ".github", "workflows", "pr-metadata.yml");

        // Act
        var workflow = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        // Assert
        workflow.ShouldContain("uses: danigutsch/PandaBear.SharedKernel/.github/workflows/pr-metadata-reusable.yml@", StringComparison.Ordinal);
        workflow.ShouldContain("source-repository: danigutsch/PandaBear.SharedKernel", StringComparison.Ordinal);
        workflow.ShouldContain("runner: ${{ vars.CI_UBUNTU_RUNNER }}", StringComparison.Ordinal);
        workflow.ShouldContain("    permissions:\n      contents: read\n    uses:", StringComparison.Ordinal);
        workflow.ShouldNotContain("secrets:", StringComparison.Ordinal);
        workflow.ShouldNotContain("pull_request_target", StringComparison.Ordinal);
        workflow.ShouldNotContain("github.event.pull_request.title", StringComparison.Ordinal);
    }
}
