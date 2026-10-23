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
        action.ShouldContain("https://github.com/danigutsch/PandaBear.SharedKernel/releases/download/v", StringComparison.Ordinal);
        action.ShouldContain("$pin.packageSha256.Keys", StringComparison.Ordinal);
        action.ShouldContain("@('candidate.json')", StringComparison.Ordinal);
        action.ShouldContain("Invoke-WebRequest", StringComparison.Ordinal);
        action.ShouldContain("-MaximumRetryCount 3", StringComparison.Ordinal);
        action.ShouldNotContain("gh release download", StringComparison.Ordinal);
        action.ShouldNotContain("GH_TOKEN:", StringComparison.Ordinal);
        action.ShouldContain("if: ${{ inputs.restore-targets != '' || inputs.restore-targets-file != '' }}", StringComparison.Ordinal);
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
        action.ShouldContain("  sharedkernel-library-root:\n", StringComparison.Ordinal);
        action.ShouldContain("  sharedkernel-candidate-feed:\n", StringComparison.Ordinal);
        action.ShouldContain("if: ${{ inputs.sharedkernel-library-root == '' }}", StringComparison.Ordinal);
        action.ShouldContain("if: ${{ inputs.sharedkernel-library-root == '' && inputs.sharedkernel-candidate-feed == '' }}", StringComparison.Ordinal);
        action.ShouldContain("LIBRARY_ROOT: ${{ inputs.sharedkernel-library-root }}", StringComparison.Ordinal);
        action.ShouldContain("CANDIDATE_FEED: ${{ inputs.sharedkernel-candidate-feed }}", StringComparison.Ordinal);
        action.ShouldContain("CANDIDATE_FEED=\"${CANDIDATE_FEED:-${GITHUB_WORKSPACE}/.local/sharedkernel-download}\"", StringComparison.Ordinal);
        action.ShouldNotContain("${{ vars.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ci.yml")]
    [InlineData("roadmap-maintenance.yml")]
    [InlineData("release-prep.yml")]
    [InlineData("devcontainer-smoke.yml")]
    public async Task Workflow_callers_pass_repository_overrides_through_explicit_action_inputs(string workflowName)
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "workflows", workflowName);
        var workflow = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        // Act
        const string actionPath = "uses: ./.github/actions/setup-ci-prerequisites";
        var calls = workflow.Split(actionPath, StringSplitOptions.None)
            .Skip(1)
            .ToArray();

        // Assert
        calls.Length.ShouldBeGreaterThan(0);
        foreach (var call in calls)
        {
            var step = call.Split("\n      - ", StringSplitOptions.None)[0];
            step.ShouldContain("        with:\n", StringComparison.Ordinal);
            step.ShouldContain("          sharedkernel-library-root: ${{ vars.SHAREDKERNEL_LIBRARY_ROOT }}", StringComparison.Ordinal);
            step.ShouldContain("          sharedkernel-candidate-feed: ${{ vars.SHAREDKERNEL_CANDIDATE_FEED }}", StringComparison.Ordinal);
        }
    }
}
