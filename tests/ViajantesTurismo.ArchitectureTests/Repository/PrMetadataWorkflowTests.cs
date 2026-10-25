using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class PrMetadataWorkflowTests
{
    [Fact]
    public async Task Pr_metadata_verifies_the_event_file_with_the_pinned_ci_tool()
    {
        // Arrange
        var path = Path.Combine(RepositoryTestProcess.GetRepositoryRoot(), ".github", "workflows", "pr-metadata.yml");

        // Act
        var workflow = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var build = workflow.IndexOf("uses: ./.github/actions/build-sharedkernel-tool", StringComparison.Ordinal);
        var verify = workflow.IndexOf(
            "dotnet run --project \"$SHAREDKERNEL_SOURCE/tools/SharedKernel.CI.Tool\" -c Release --no-build -- verify-commit-message --event \"$GITHUB_EVENT_PATH\"",
            StringComparison.Ordinal);

        // Assert
        build.ShouldBeGreaterThan(-1);
        verify.ShouldBeGreaterThan(build);
        workflow.ShouldContain("tool: SharedKernel.CI.Tool", StringComparison.Ordinal);
        workflow.ShouldContain("persist-credentials: false", StringComparison.Ordinal);
        workflow.ShouldNotContain("github.event.pull_request.title", StringComparison.Ordinal);
    }
}
