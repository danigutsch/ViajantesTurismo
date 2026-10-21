namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class ConsumerSetupContractTests
{
    [Fact]
    public async Task Devcontainer_ci_stages_candidate_before_starting_container()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "workflows", "devcontainer-smoke.yml");

        // Act
        var workflow = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var prerequisites = workflow.IndexOf("uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        var container = workflow.IndexOf("- name: Run devcontainer validation script", StringComparison.Ordinal);

        // Assert
        prerequisites.ShouldBeGreaterThan(-1);
        container.ShouldBeGreaterThan(prerequisites);
        workflow.ShouldContain("restore-local-tools: \"false\"", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("setup-dev.sh", "restore_dotnet_dependencies\n")]
    [InlineData("setup-dev.ps1", "Restore-DotNetDependencies\n")]
    [InlineData(".devcontainer/on-create.sh", "dotnet tool restore")]
    public async Task Setup_requires_staged_candidate_before_restoring_dependencies(string script, string restoreCommand)
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, script);

        // Act
        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var stagedManifest = content.IndexOf(".local/sharedkernel-feed/candidate.json", StringComparison.Ordinal);
        var restore = content.LastIndexOf(restoreCommand, StringComparison.Ordinal);

        // Assert
        stagedManifest.ShouldBeGreaterThan(-1);
        restore.ShouldBeGreaterThan(stagedManifest);
        content.ShouldContain("bootstrap-sharedkernel --library-root", StringComparison.Ordinal);
        content.ShouldContain("docs/sharedkernel/development.md", StringComparison.Ordinal);
    }
}
