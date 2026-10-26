using System.Text.Json;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class ConsumerSetupContractTests
{
    [Fact]
    public async Task Devcontainer_ci_stages_candidate_and_tools_before_validating_the_container()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "workflows", "devcontainer-smoke.yml");

        // Act
        var workflow = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var prerequisites = workflow.IndexOf("uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        var smoke = workflow.IndexOf("if: steps.validation-mode.outputs.mode == 'smoke'\n        run: dotnet tool run sharedkernel-repo -- verify-devcontainer --exec \"$VERIFY_TOOLCHAINS\" --root .", StringComparison.Ordinal);
        var full = workflow.IndexOf("if: steps.validation-mode.outputs.mode == 'full'", StringComparison.Ordinal);
        var fullStep = workflow[full..].Split("\n\n", StringSplitOptions.None)[0];

        // Assert
        prerequisites.ShouldBeGreaterThan(-1);
        smoke.ShouldBeGreaterThan(prerequisites);
        full.ShouldBeGreaterThan(prerequisites);
        fullStep.ShouldContain("verify-devcontainer --exec \"$VERIFY_TOOLCHAINS\"", StringComparison.Ordinal);
        fullStep.ShouldContain("--exec \"$INSTALL_PLAYWRIGHT_DEPENDENCIES\" --exec \"$RUN_TEST_SLICES\" --root .", StringComparison.Ordinal);
        workflow.ShouldContain("dotnet tool run sharedkernel-ci -- select --selection \"${selection}\"", StringComparison.Ordinal);
        workflow.ShouldContain(
            "dotnet tool run sharedkernel-ci -- run --slice \"${slice}\" --selection \"${selection}\" --install-playwright \\\n            --exclude-trait Requires=fasttext",
            StringComparison.Ordinal);
        workflow.ShouldContain("    paths:\n      - .devcontainer/**\n      - .github/workflows/devcontainer-smoke.yml\n      - global.json\n      - setup-dev.sh\n  schedule:", StringComparison.Ordinal);
        workflow.ShouldContain("path: TestResults/devcontainer-smoke/**", StringComparison.Ordinal);
        workflow.ShouldNotContain("restore-local-tools", StringComparison.Ordinal);
        workflow.ShouldNotContain("scripts/", StringComparison.Ordinal);
        workflow.Split("uses: actions/checkout@", StringSplitOptions.None).Skip(1)
            .Where(static checkout => !checkout.Split("\n      - ", StringSplitOptions.None)[0].Contains("persist-credentials: false", StringComparison.Ordinal))
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData("setup-dev.sh", "restore_dotnet_dependencies\n", "(--repository danigutsch/PandaBear.SharedKernel)")]
    [InlineData("setup-dev.ps1", "Restore-DotNetDependencies\n", "'--repository', 'danigutsch/PandaBear.SharedKernel'")]
    public async Task Setup_stages_the_pinned_candidate_from_a_sparse_ci_tool_build_before_restoring(string script, string restoreCommand, string defaultSource)
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
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
    }

    [Fact]
    public async Task Devcontainer_marks_the_tool_checkout_safe_before_running_setup()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".devcontainer", "on-create.sh");

        // Act
        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var safeDirectory = content.IndexOf("\"${WORKSPACE_FOLDER}/.local/sharedkernel-source/SharedKernel.CI.Tool\"", StringComparison.Ordinal);
        var setup = content.IndexOf("bash \"${WORKSPACE_FOLDER}/setup-dev.sh\"", StringComparison.Ordinal);

        // Assert
        safeDirectory.ShouldBeGreaterThan(-1);
        setup.ShouldBeGreaterThan(safeDirectory);
    }

    [Fact]
    public async Task Devcontainer_image_is_pinned_by_digest_alone()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();

        // Act
        var configuration = await File.ReadAllTextAsync(Path.Combine(root, ".devcontainer", "devcontainer.json"), TestContext.Current.CancellationToken);
        var image = configuration.Split('\n').Where(static line => line.TrimStart().StartsWith("\"image\":", StringComparison.Ordinal)).ShouldHaveSingleItem();

        // Assert
        image.ShouldMatch("^\\s*\"image\": \"[a-z0-9./-]+@sha256:[0-9a-f]{64}\",$");
    }

    [Fact]
    public async Task Devcontainer_setup_gives_the_user_the_local_share_directory_that_the_k6_library_materializes_into()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();

        // Act
        var onCreate = await File.ReadAllTextAsync(Path.Combine(root, ".devcontainer", "on-create.sh"), TestContext.Current.CancellationToken);

        // Assert
        onCreate.ShouldContain("sudo chown vscode:vscode /home/vscode/.local /home/vscode/.local/share", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Minimal_environment_builds_keep_the_container_flag_that_selects_the_container_build_paths()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();

        // Act
        var props = await File.ReadAllTextAsync(Path.Combine(root, "Directory.Build.props"), TestContext.Current.CancellationToken);
        var postStart = await File.ReadAllTextAsync(Path.Combine(root, ".devcontainer", "post-start.sh"), TestContext.Current.CancellationToken);
        var configuration = await File.ReadAllTextAsync(Path.Combine(root, ".config", "sharedkernel-ci.json"), TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(configuration);
        var targets = document.RootElement.GetProperty("openApi").EnumerateArray().ToArray();

        // Assert
        props.ShouldContain("Condition=\"'$(REMOTE_CONTAINERS)' == 'true'\"", StringComparison.Ordinal);
        props.ShouldNotContain("viajantesturismo-devcontainer", StringComparison.Ordinal);
        postStart.ShouldNotContain("viajantesturismo-devcontainer", StringComparison.Ordinal);
        targets.ShouldNotBeEmpty();
        targets.ShouldAllSatisfy(static target => target.GetProperty("preserveEnvironment").EnumerateArray().Select(static name => name.GetString()).ShouldContain("REMOTE_CONTAINERS"));
    }

    [Fact]
    public async Task Hosted_prerequisites_build_the_ci_tool_and_stage_the_exact_candidate_before_restoring_tools()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "actions", "setup-ci-prerequisites", "action.yml");

        // Act
        var action = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var build = action.IndexOf("uses: ./.github/actions/build-sharedkernel-tool", StringComparison.Ordinal);
        var stage = action.IndexOf("- name: Stage pinned SharedKernel candidate", StringComparison.Ordinal);
        var restore = action.IndexOf("- name: Restore .NET local tools", StringComparison.Ordinal);

        // Assert
        build.ShouldBeGreaterThan(-1);
        stage.ShouldBeGreaterThan(build);
        restore.ShouldBeGreaterThan(stage);
        action.ShouldContain("        tool: SharedKernel.CI.Tool\n        library-root: ${{ inputs.sharedkernel-library-root }}\n", StringComparison.Ordinal);
        action.ShouldContain("stage-candidate --pin .config/sharedkernel-candidate.json --library-root $env:SHAREDKERNEL_SOURCE", StringComparison.Ordinal);
        action.ShouldContain("--output .local/sharedkernel-feed @source", StringComparison.Ordinal);
        action.ShouldContain("$source = '--repository', 'danigutsch/PandaBear.SharedKernel'", StringComparison.Ordinal);
        action.ShouldNotContain("restore-targets", StringComparison.Ordinal);
        action.ShouldNotContain("- name: Restore .NET dependencies", StringComparison.Ordinal);
        action.ShouldNotContain("Invoke-WebRequest", StringComparison.Ordinal);
        action.ShouldNotContain("GH_TOKEN", StringComparison.Ordinal);
        action.ShouldNotContain("token", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Hosted_provisioning_preserves_explicit_local_library_and_feed_overrides()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
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
        var root = RepositoryTestProcess.GetRepositoryRoot();
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
        var root = RepositoryTestProcess.GetRepositoryRoot();
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
