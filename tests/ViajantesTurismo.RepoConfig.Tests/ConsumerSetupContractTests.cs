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

    [Fact]
    public async Task Hosted_prerequisites_provision_the_exact_candidate_before_bootstrap_and_restore()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "setup-ci-prerequisites", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var checkout = action.IndexOf("- name: Checkout pinned SharedKernel source", StringComparison.Ordinal);
        var download = action.IndexOf("- name: Download pinned SharedKernel candidate", StringComparison.Ordinal);
        var bootstrap = action.IndexOf("- name: Stage pinned SharedKernel candidate", StringComparison.Ordinal);
        var restore = action.IndexOf("- name: Restore .NET dependencies", StringComparison.Ordinal);

        // Assert
        checkout.ShouldBeGreaterThan(-1);
        download.ShouldBeGreaterThan(checkout);
        bootstrap.ShouldBeGreaterThan(download);
        restore.ShouldBeGreaterThan(bootstrap);
        action.ShouldContain("repository: danigutsch/PandaBear.SharedKernel", StringComparison.Ordinal);
        action.ShouldContain("ref: ${{ steps.sharedkernel-pin.outputs.revision }}", StringComparison.Ordinal);
        action.ShouldContain("persist-credentials: false", StringComparison.Ordinal);
        action.ShouldContain("gh release download \"v${PINNED_VERSION}\"", StringComparison.Ordinal);
        action.ShouldContain("--pattern '*.nupkg'", StringComparison.Ordinal);
        action.ShouldContain("--pattern 'candidate.json'", StringComparison.Ordinal);
        action.ShouldContain("GH_TOKEN: ${{ github.token }}", StringComparison.Ordinal);
        action.ShouldNotContain("--clobber", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hosted_provisioning_preserves_explicit_local_library_and_feed_overrides()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "setup-ci-prerequisites", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        // Assert
        action.ShouldContain("if: ${{ vars.SHAREDKERNEL_LIBRARY_ROOT == '' }}", StringComparison.Ordinal);
        action.ShouldContain("if: ${{ vars.SHAREDKERNEL_LIBRARY_ROOT == '' && vars.SHAREDKERNEL_CANDIDATE_FEED == '' }}", StringComparison.Ordinal);
        action.ShouldContain("LIBRARY_ROOT: ${{ vars.SHAREDKERNEL_LIBRARY_ROOT }}", StringComparison.Ordinal);
        action.ShouldContain("CANDIDATE_FEED: ${{ vars.SHAREDKERNEL_CANDIDATE_FEED }}", StringComparison.Ordinal);
        action.ShouldContain("CANDIDATE_FEED=\"${CANDIDATE_FEED:-${GITHUB_WORKSPACE}/.local/sharedkernel-download}\"", StringComparison.Ordinal);
    }
}
