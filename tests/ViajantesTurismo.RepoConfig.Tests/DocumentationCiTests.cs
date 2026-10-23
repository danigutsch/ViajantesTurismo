namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class DocumentationCiTests
{
    [Theory]
    [InlineData("direct", "docs/guide.md", true, false)]
    [InlineData("merge-base", "docs/guide.md", true, false)]
    [InlineData("direct", "CONTRIBUTING.md", true, false)]
    [InlineData("direct", "scripts/lint-all.sh", false, false)]
    [InlineData("direct", "docs/restore.targets", false, true)]
    [InlineData("direct", "src/App/README.md", false, true)]
    public async Task Documentation_selection_preserves_checks_for_scripts_build_inputs_and_package_readmes(
        string mode, string path, bool documentationOnly, bool buildRequired)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        repository.WriteFile(path, "before");
        var token = TestContext.Current.CancellationToken;
        var baseline = await repository.CommitFiles(token);
        repository.WriteFile(path, "after");
        var head = await repository.CommitFiles(token);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["select-ci-test-projects", "--mode", mode,
            "--base", baseline, "--head", head, "--github-output", "outputs.txt"], output, error, repository.RootPath, token);
        var outputs = await File.ReadAllTextAsync(Path.Combine(repository.RootPath, "outputs.txt"), token);

        // Assert
        result.ShouldBe(0);
        outputs.ShouldContain("build_required=" + (buildRequired ? "true" : "false"), StringComparison.Ordinal);
        outputs.ShouldContain("documentation_only=" + (documentationOnly ? "true" : "false"), StringComparison.Ordinal);
        if (documentationOnly)
        {
            outputs.ShouldContain("selected_test_project_count=0", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Documentation_workflow_avoids_hidden_test_runs_and_candidate_bootstrap()
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var detection = workflow.Split("  detect-changes:", StringSplitOptions.None)[1]
            .Split("\n  calculate-version:", StringSplitOptions.None)[0];
        var version = workflow.Split("  calculate-version:", StringSplitOptions.None)[1]
            .Split("\n  openapi-tool-windows:", StringSplitOptions.None)[0];
        var lint = workflow.Split("  lint:", StringSplitOptions.None)[1];
        var slices = workflow.Split("  test-slices:", StringSplitOptions.None)[1]
            .Split("\n  build-and-test:", StringSplitOptions.None)[0];

        // Assert
        detection.ShouldNotContain("setup-ci-prerequisites", StringComparison.Ordinal);
        detection.ShouldContain("github.event_name == 'push'", StringComparison.Ordinal);
        detection.ShouldContain("'direct'", StringComparison.Ordinal);
        detection.ShouldContain("github.event.before", StringComparison.Ordinal);
        version.ShouldContain("needs: detect-changes", StringComparison.Ordinal);
        version.ShouldContain("needs.detect-changes.outputs.build_required == 'true'", StringComparison.Ordinal);
        lint.ShouldContain("needs: detect-changes", StringComparison.Ordinal);
        lint.ShouldContain("if: needs.detect-changes.outputs.documentation_only != 'true'\n        uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        lint.ShouldContain("if: needs.detect-changes.outputs.documentation_only != 'true'\n        run: bash scripts/lint-all.sh", StringComparison.Ordinal);
        lint.ShouldContain("python3 -B scripts/lint-links.py", StringComparison.Ordinal);
        lint.ShouldNotContain("bash scripts/lint-links.sh", StringComparison.Ordinal);
        slices.ShouldContain("needs.detect-changes.outputs.build_required == 'true'", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("true", "success", "success", 0)]
    [InlineData("true", "failure", "success", 1)]
    [InlineData("true", "success", "failure", 1)]
    [InlineData("false", "skipped", "skipped", 0)]
    [InlineData("false", "failure", "skipped", 1)]
    [InlineData("", "skipped", "skipped", 1)]
    public async Task Quality_gate_accepts_documentation_skips_but_rejects_failures_and_missing_selection(
        string required, string tests, string build, int expected)
    {
        // Arrange
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var workflow = await File.ReadAllTextAsync(Path.Combine(root, ".github/workflows/ci.yml"), TestContext.Current.CancellationToken);
        var step = workflow.Split("      - name: Require successful change selection and tests\n", StringSplitOptions.None)[1]
            .Split("      - name: Checkout repository", StringSplitOptions.None)[0];
        var script = step.Split("        run: |\n", StringSplitOptions.None)[1];
        using var workspace = new TemporaryRepoConfigWorkspace();
        workspace.WriteFile("gate.sh", "set -e\n" + script);

        // Act
        var result = await CiTestSelectionTestProcess.Run("bash", workspace.RootPath, ["gate.sh"],
            TestContext.Current.CancellationToken, new Dictionary<string, string?>
            {
                ["SELECTION_RESULT"] = "success",
                ["TEST_RESULT"] = tests,
                ["BUILD_RESULT"] = build,
                ["BUILD_REQUIRED"] = required
            });

        // Assert
        result.ExitCode.ShouldBe(expected);
    }
}
