using System.Text.Json;
using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiRunManifestTests
{
    [Theory]
    [InlineData("true", "failure", "cancelled", JsonValueKind.True, true)]
    [InlineData("false", "skipped", "success", JsonValueKind.False, true)]
    [InlineData("", "skipped", "failure", JsonValueKind.Null, true)]
    [InlineData("unexpected", "skipped", "failure", JsonValueKind.Null, true)]
    [InlineData("true", "success", "success", JsonValueKind.True, false)]
    public async Task Manifest_records_the_workflow_attempt_and_its_job_results(
        string buildRequired, string testResult, string gateResult, JsonValueKind buildKind, bool hasPullRequest)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var repository = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var workflow = await File.ReadAllTextAsync(Path.Combine(repository, ".github/workflows/ci.yml"), ct);
        const string manifestStep = "      - name: Write CI run manifest";
        var manifestStart = workflow.IndexOf(manifestStep, StringComparison.Ordinal);
        manifestStart.ShouldBeGreaterThanOrEqualTo(0);
        var script = workflow[manifestStart..]
            .Split("        run: |\n", StringSplitOptions.None)[1]
            .Split("      - name: Upload CI run manifest", StringSplitOptions.None)[0];
        using var workspace = new TemporaryWorkspace();
        workspace.WriteFile("manifest.sh", "set -e\n" + script);
        var eventName = hasPullRequest ? "pull_request" : "merge_group";
        var pullRequestHead = hasPullRequest ? "febdf46577b3fbf185123dcae7c1c6526fee9bf1" : null;

        // Act
        var outcome = await TestProcess.Run("bash", workspace.RootPath, ["manifest.sh"], ct,
            new Dictionary<string, string?>
            {
                ["GITHUB_WORKFLOW"] = "CI",
                ["GITHUB_RUN_ID"] = "123456",
                ["GITHUB_RUN_ATTEMPT"] = "2",
                ["GITHUB_EVENT_NAME"] = eventName,
                ["GITHUB_SHA"] = "bdaf5366db2f94d124b955354b428d63a71bc7d3",
                ["PR_HEAD_SHA"] = pullRequestHead ?? string.Empty,
                ["BUILD_REQUIRED"] = buildRequired,
                ["DETECT_RESULT"] = buildKind == JsonValueKind.Null ? "failure" : "success",
                ["VERSION_RESULT"] = "skipped",
                ["TEST_RESULT"] = testResult,
                ["BUILD_AND_TEST_RESULT"] = "success",
                ["SOLUTION_BUILD_RESULT"] = "skipped",
                ["FORMAT_RESULT"] = "skipped",
                ["QUALITY_GATE_RESULT"] = gateResult,
                ["WARNING_SUMMARY_RESULT"] = "success",
                ["LINT_RESULT"] = "success",
                ["UNRELATED_SECRET"] = "must-not-be-written",
            });

        // Assert
        outcome.ExitCode.ShouldBe(0);
        var output = await File.ReadAllTextAsync(Path.Combine(workspace.RootPath, "TestResults", "ci-manifest.json"), ct);
        using var manifest = JsonDocument.Parse(output);
        var root = manifest.RootElement;
        root.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        root.GetProperty("workflow").GetString().ShouldBe("CI");
        root.GetProperty("runId").GetString().ShouldBe("123456");
        root.GetProperty("runAttempt").GetInt32().ShouldBe(2);
        root.GetProperty("event").GetString().ShouldBe(eventName);
        root.GetProperty("checkedCommit").GetString().ShouldBe("bdaf5366db2f94d124b955354b428d63a71bc7d3");
        root.GetProperty("pullRequestHead").GetString().ShouldBe(pullRequestHead);
        root.GetProperty("buildRequired").ValueKind.ShouldBe(buildKind);
        var jobs = root.GetProperty("jobResults");
        jobs.GetProperty("test-slices").GetString().ShouldBe(testResult);
        jobs.GetProperty("quality-gate").GetString().ShouldBe(gateResult);
        output.ShouldNotContain("UNRELATED_SECRET", StringComparison.Ordinal);
        output.ShouldNotContain("must-not-be-written", StringComparison.Ordinal);
    }
}
