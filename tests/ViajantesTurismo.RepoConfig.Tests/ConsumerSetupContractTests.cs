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
    [InlineData("setup-dev.sh", "restore_dotnet_dependencies\n", "(--repository danigutsch/PandaBear.SharedKernel)")]
    [InlineData("setup-dev.ps1", "Restore-DotNetDependencies\n", "'--repository', 'danigutsch/PandaBear.SharedKernel'")]
    public async Task Setup_stages_the_pinned_candidate_from_a_sparse_ci_tool_build_before_restoring(string script, string restoreCommand, string defaultSource)
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, script);

        // Act
        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var stage = content.IndexOf("stage-candidate --pin .config/sharedkernel-candidate.json", StringComparison.Ordinal);
        var restore = content.LastIndexOf(restoreCommand, StringComparison.Ordinal);

        // Assert
        stage.ShouldBeGreaterThan(-1);
        restore.ShouldBeGreaterThan(stage);
        content.ShouldContain("--output .local/sharedkernel-feed", StringComparison.Ordinal);
        content.ShouldContain(defaultSource, StringComparison.Ordinal);
        content.ShouldContain("git clone --filter=blob:none --no-checkout https://github.com/danigutsch/PandaBear.SharedKernel.git", StringComparison.Ordinal);
        content.ShouldContain("sparse-checkout set --cone tools/SharedKernel.CI.Tool tools/Shared", StringComparison.Ordinal);
        content.ShouldContain(".local/sharedkernel-source/SharedKernel.CI.Tool", StringComparison.Ordinal);
        content.ShouldContain("remote.origin.promisor", StringComparison.Ordinal);
        content.ShouldContain("dotnet restore tools/SharedKernel.CI.Tool --locked-mode", StringComparison.Ordinal);
        content.ShouldContain("dotnet build tools/SharedKernel.CI.Tool -c Release --no-restore", StringComparison.Ordinal);
        content.ShouldNotContain("bootstrap-sharedkernel", StringComparison.Ordinal);
        content.ShouldNotContain(".local/sharedkernel-library", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Devcontainer_marks_the_tool_checkout_safe_before_running_setup()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".devcontainer", "on-create.sh");

        // Act
        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var safeDirectory = content.IndexOf("\"${WORKSPACE_FOLDER}/.local/sharedkernel-source/SharedKernel.CI.Tool\"", StringComparison.Ordinal);
        var setup = content.IndexOf("bash \"${WORKSPACE_FOLDER}/setup-dev.sh\"", StringComparison.Ordinal);

        // Assert
        safeDirectory.ShouldBeGreaterThan(-1);
        setup.ShouldBeGreaterThan(safeDirectory);
        content.ShouldNotContain(".local/sharedkernel-feed/candidate.json", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hosted_prerequisites_build_the_ci_tool_and_stage_the_exact_candidate_before_restore()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "setup-ci-prerequisites", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var build = action.IndexOf("uses: ./.github/actions/build-sharedkernel-tool", StringComparison.Ordinal);
        var stage = action.IndexOf("- name: Stage pinned SharedKernel candidate", StringComparison.Ordinal);
        var restore = action.IndexOf("- name: Restore .NET dependencies", StringComparison.Ordinal);

        // Assert
        build.ShouldBeGreaterThan(-1);
        stage.ShouldBeGreaterThan(build);
        restore.ShouldBeGreaterThan(stage);
        action.ShouldContain("        tool: SharedKernel.CI.Tool\n        library-root: ${{ inputs.sharedkernel-library-root }}\n", StringComparison.Ordinal);
        action.ShouldContain("stage-candidate --pin .config/sharedkernel-candidate.json --library-root $env:SHAREDKERNEL_SOURCE", StringComparison.Ordinal);
        action.ShouldContain("--output .local/sharedkernel-feed @source", StringComparison.Ordinal);
        action.ShouldContain("$source = '--repository', 'danigutsch/PandaBear.SharedKernel'", StringComparison.Ordinal);
        action.ShouldContain("if: ${{ inputs.restore-targets != '' || inputs.restore-targets-file != '' }}", StringComparison.Ordinal);
        action.ShouldNotContain("Invoke-WebRequest", StringComparison.Ordinal);
        action.ShouldNotContain(".local/sharedkernel-library", StringComparison.Ordinal);
        action.ShouldNotContain("bootstrap-sharedkernel", StringComparison.Ordinal);
        action.ShouldNotContain("dotnet-global-json", StringComparison.Ordinal);
        action.ShouldNotContain("GH_TOKEN", StringComparison.Ordinal);
        action.ShouldNotContain("token", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Hosted_provisioning_preserves_explicit_local_library_and_feed_overrides()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "setup-ci-prerequisites", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var feed = action.IndexOf("$source = '--feed', $env:CANDIDATE_FEED", StringComparison.Ordinal);
        var libraryFeed = action.IndexOf("$source = '--feed', (Join-Path $env:LIBRARY_ROOT \"artifacts/api-compat/packages/$($pin.packageVersion)\")", StringComparison.Ordinal);
        var repository = action.IndexOf("$source = '--repository'", StringComparison.Ordinal);

        // Assert
        action.ShouldContain("  sharedkernel-library-root:\n", StringComparison.Ordinal);
        action.ShouldContain("  sharedkernel-candidate-feed:\n", StringComparison.Ordinal);
        action.ShouldContain("LIBRARY_ROOT: ${{ inputs.sharedkernel-library-root }}", StringComparison.Ordinal);
        action.ShouldContain("CANDIDATE_FEED: ${{ inputs.sharedkernel-candidate-feed }}", StringComparison.Ordinal);
        feed.ShouldBeGreaterThan(-1);
        libraryFeed.ShouldBeGreaterThan(feed);
        repository.ShouldBeGreaterThan(libraryFeed);
        action.ShouldNotContain("${{ vars.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tool_build_action_checks_out_only_the_pinned_sparse_tool_source_without_credentials()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "build-sharedkernel-tool", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var runSteps = action.Split("\n", StringSplitOptions.None).Count(static line => line.TrimStart().StartsWith("run: ", StringComparison.Ordinal));
        var shells = action.Split("\n", StringSplitOptions.None).Count(static line => line.TrimStart().StartsWith("shell: ", StringComparison.Ordinal));

        // Assert
        action.ShouldContain("if ($env:TOOL -cne 'SharedKernel.CI.Tool' -and $env:TOOL -cne 'SharedKernel.RepoConfig.Tool') {", StringComparison.Ordinal);
        action.ShouldContain("Get-Content -LiteralPath .config/sharedkernel-candidate.json -Raw", StringComparison.Ordinal);
        action.ShouldContain("if ($revision -cnotmatch '^[0-9a-f]{40}$') {", StringComparison.Ordinal);
        action.ShouldContain("      if: ${{ inputs.library-root == '' }}\n      uses: actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6\n", StringComparison.Ordinal);
        action.ShouldContain("        repository: danigutsch/PandaBear.SharedKernel\n", StringComparison.Ordinal);
        action.ShouldContain("        ref: ${{ steps.sharedkernel-source.outputs.revision }}\n", StringComparison.Ordinal);
        action.ShouldContain("        path: .local/sharedkernel-source/${{ inputs.tool }}\n", StringComparison.Ordinal);
        action.ShouldContain("        persist-credentials: false\n", StringComparison.Ordinal);
        action.ShouldContain("        sparse-checkout-cone-mode: true\n        sparse-checkout: |\n          tools/${{ inputs.tool }}\n          tools/Shared\n\n", StringComparison.Ordinal);
        action.ShouldContain("git -C $source rev-parse --show-cdup", StringComparison.Ordinal);
        action.ShouldContain("$head -cne $revision", StringComparison.Ordinal);
        action.ShouldContain("git -C $source status --porcelain", StringComparison.Ordinal);
        action.ShouldContain("WORKSPACE: ${{ github.workspace }}", StringComparison.Ordinal);
        action.ShouldContain("\"SHAREDKERNEL_SOURCE=$source\" >> $env:GITHUB_ENV", StringComparison.Ordinal);
        action.ShouldContain("dotnet restore \"tools/$env:TOOL\" --locked-mode", StringComparison.Ordinal);
        action.ShouldContain("dotnet build \"tools/$env:TOOL\" -c Release --no-restore", StringComparison.Ordinal);
        shells.ShouldBe(runSteps);
        action.ShouldNotContain("token", StringComparison.OrdinalIgnoreCase);
        action.ShouldNotContain("secrets.", StringComparison.Ordinal);
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
