using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class DocumentationCiTests
{
    [Fact]
    public void Documentation_workflow_avoids_hidden_test_runs_and_candidate_bootstrap()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var detection = workflow.Split("  detect-changes:", StringSplitOptions.None)[1]
            .Split("\n  calculate-version:", StringSplitOptions.None)[0];
        var version = workflow.Split("  calculate-version:", StringSplitOptions.None)[1]
            .Split("\n  test-slices:", StringSplitOptions.None)[0];
        var lint = workflow.Split("  lint:", StringSplitOptions.None)[1];
        var slices = workflow.Split("  test-slices:", StringSplitOptions.None)[1]
            .Split("\n  build-and-test:", StringSplitOptions.None)[0];

        // Assert
        detection.ShouldNotContain("setup-ci-prerequisites", StringComparison.Ordinal);
        detection.ShouldContain("pull_request) BASE_SHA=\"$(git rev-parse HEAD^1)\" ;;", StringComparison.Ordinal);
        detection.ShouldContain("push) BASE_SHA=\"$PUSH_BEFORE\" ;;", StringComparison.Ordinal);
        detection.ShouldContain("merge_group) BASE_SHA=\"$MERGE_GROUP_BASE\" ;;", StringComparison.Ordinal);
        detection.ShouldContain("PUSH_BEFORE: ${{ github.event.before }}", StringComparison.Ordinal);
        detection.ShouldContain("MERGE_GROUP_BASE: ${{ github.event.merge_group.base_sha }}", StringComparison.Ordinal);
        detection.ShouldContain("select --base \"$BASE_SHA\" --github-output \"$GITHUB_OUTPUT\"", StringComparison.Ordinal);
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
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflow = await File.ReadAllTextAsync(Path.Combine(root, ".github/workflows/ci.yml"), TestContext.Current.CancellationToken);
        var step = workflow.Split("      - name: Require successful change selection and tests\n", StringSplitOptions.None)[1]
            .Split("      - name: Checkout repository", StringSplitOptions.None)[0];
        var script = step.Split("        run: |\n", StringSplitOptions.None)[1];
        using var workspace = new TemporaryWorkspace();
        workspace.WriteFile("gate.sh", "set -e\n" + script);

        // Act
        var result = await RepositoryTestProcess.Run("bash", workspace.RootPath, ["gate.sh"],
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
