namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiWorkflowCommandTests
{
    [Theory]
    [InlineData("src/App/App.csproj", "<Project>")]
    [InlineData("Directory.Build.props", "<Project>")]
    [InlineData("Directory.Build.targets", "<Project>")]
    public async Task Unreadable_build_graph_keeps_full_review_after_documentation_only_changes(string path, string content)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile(path, content);
        workspace.WriteFile("docs/guide.md", "before");
        var token = TestContext.Current.CancellationToken;
        var baseline = await workspace.CommitFiles(token);
        workspace.WriteFile("docs/guide.md", "after");
        var head = await workspace.CommitFiles(token);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["plan-dependency-review", "--base", baseline, "--head", head,
            "--github-output", "github-output.txt"], output, error, workspace.RootPath, token);
        var githubOutput = await File.ReadAllTextAsync(Path.Combine(workspace.RootPath, "github-output.txt"), token);
        using var evidence = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(workspace.RootPath, "TestResults/dependency-review-plan.json"), token));

        // Assert
        result.ShouldBe(0);
        output.ToString().ShouldContain("review_required=true", StringComparison.Ordinal);
        githubOutput.ShouldContain("review_required=true", StringComparison.Ordinal);
        evidence.RootElement.GetProperty("reviewRequired").GetBoolean().ShouldBeTrue();
        evidence.RootElement.GetProperty("reason").GetString().ShouldContain("complete dependency review is required", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pr_metadata_verifies_the_event_file_with_the_pinned_ci_tool()
    {
        // Arrange
        var path = Path.Combine(CiTestSelectionTestProcess.GetRepositoryRoot(), ".github", "workflows", "pr-metadata.yml");

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

    [Fact]
    public async Task Standalone_documentation_does_not_require_dependency_snapshots()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("docs/guide.md", "before");
        var baseline = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        workspace.WriteFile("docs/guide.md", "after");
        var head = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["plan-dependency-review", "--base", baseline, "--head", head],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
        output.ToString().ShouldContain("review_required=false", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_evidence_is_bound_to_revision_paths_and_file_hashes()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("src/App/App.cs", "// source");
        await workspace.CommitFiles(TestContext.Current.CancellationToken);
        workspace.WriteFile("src/App/bin/Release/net10.0/App.pdb", "symbols");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var token = TestContext.Current.CancellationToken;

        // Act
        var write = await RepoConfigToolApplication.Run(["write-build-evidence"], output, error, workspace.RootPath, token);
        var verify = await RepoConfigToolApplication.Run(["verify-build-evidence"], output, error, workspace.RootPath, token);
        workspace.WriteFile("src/App/bin/Release/net10.0/App.pdb", "changed symbols");
        var changed = await RepoConfigToolApplication.Run(["verify-build-evidence"], output, error, workspace.RootPath, token);

        // Assert
        write.ShouldBe(0);
        verify.ShouldBe(0);
        changed.ShouldBe(1);
    }

    [Theory]
    [InlineData("Directory.Packages.props")]
    [InlineData(".config/sharedkernel-candidate.json")]
    [InlineData("src/App/README.md")]
    [InlineData("src/App/App.cs")]
    [InlineData("unknown.file")]
    public async Task Unclassified_and_dependency_inputs_keep_the_complete_security_gate(string path)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("docs/guide.md", "baseline");
        var baseline = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        workspace.WriteFile(path, "changed");
        var head = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["plan-dependency-review", "--base", baseline, "--head", head],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
        output.ToString().ShouldContain("review_required=true", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_comparison_history_requires_review_instead_of_claiming_no_changes()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("docs/guide.md", "baseline");
        await workspace.CommitFiles(TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["plan-dependency-review", "--base", new string('a', 40), "--head", new string('b', 40)],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
        output.ToString().ShouldContain("review_required=true", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unresolved_build_imports_prevent_documentation_from_skipping_security_review()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.AddProject("src/App/App.csproj");
        workspace.WriteFile("src/App/App.csproj", """<Project><Import Project="$(Unknown)/restore.targets" /></Project>""");
        workspace.WriteFile("docs/guide.md", "before");
        var baseline = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        workspace.WriteFile("docs/guide.md", "after");
        var head = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["plan-dependency-review", "--base", baseline, "--head", head],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
        output.ToString().ShouldContain("review_required=true", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Evidence_for_a_previous_checkout_revision_is_rejected()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("src/App/App.cs", "// original");
        await workspace.CommitFiles(TestContext.Current.CancellationToken);
        workspace.WriteFile("src/App/bin/Release/net10.0/App.pdb", "symbols");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var token = TestContext.Current.CancellationToken;
        var write = await RepoConfigToolApplication.Run(["write-build-evidence"], output, error, workspace.RootPath, token);
        workspace.WriteFile("src/App/App.cs", "// changed");
        await workspace.CommitFiles(token);

        // Act
        var verify = await RepoConfigToolApplication.Run(["verify-build-evidence"], output, error, workspace.RootPath, token);

        // Assert
        write.ShouldBe(0);
        verify.ShouldBe(1);
        error.ToString().ShouldContain("revision", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Slice_manifests_cannot_execute_projects_outside_the_solution()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.AddProject("src/App/App.csproj");
        workspace.WriteFile("projects.txt", "../outside/Outside.csproj");
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["run-ci-test-slice", "--slice-name", "guard", "--projects-file", "projects.txt"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        error.ToString().ShouldContain("repository solution", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Symbol_manifests_cannot_hash_files_outside_the_checkout()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        using var outside = new TemporaryCiTestRepository();
        workspace.WriteFile("src/App/App.cs", "// source");
        var revision = await workspace.CommitFiles(TestContext.Current.CancellationToken);
        outside.WriteFile("bin/Release/outside.pdb", "symbols");
        var path = "src/../../" + Path.GetFileName(outside.RootPath) + "/bin/Release/outside.pdb";
        var files = new Dictionary<string, string> { [path] = "not-allowed" };
        workspace.WriteFile("TestResults/build-evidence.json", System.Text.Json.JsonSerializer.Serialize(new
        {
            revision,
            files
        }));
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["verify-build-evidence"], output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        error.ToString().ShouldContain("escapes the checkout", StringComparison.Ordinal);
    }
}
