using System.Globalization;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
[Collection("Repo config tool environment")]
public sealed class RoadmapAutomationCommandTests
{
    [Fact]
    public async Task Unknown_automation_command_is_a_syntax_error_without_ci_environment()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        using var repository = new EnvironmentVariableScope("GITHUB_REPOSITORY", null);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["automation", "unknown", "--root", workspace.RootPath],
            output,
            error,
            workspace.RootPath,
            TestContext.Current.CancellationToken);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(2);
        errorText.ShouldContain("Unknown automation command: unknown", StringComparison.Ordinal);
        errorText.ShouldNotContain("GITHUB_REPOSITORY", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Plan_command_writes_only_integrity_bound_workflow_outputs()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        RoadmapAutomationTestSetup.Configure(workspace);
        IReadOnlyList<GitHubRoadmapReconcileIssue> snapshot =
        [
            GitHubRoadmapSnapshotTestOperations.Issue(
                100,
                "untrusted\n::warning::title",
                "CLOSED",
                labels: ["untrusted-label"])
        ];
        using var handler = new TestHttpMessageHandler();
        GitHubRoadmapSnapshotTestOperations.Enqueue(handler, snapshot);
        using var httpClient = new HttpClient(handler);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        var githubOutputPath = Path.Combine(workspace.RootPath, "github-output.txt");
        var githubSummaryPath = Path.Combine(workspace.RootPath, "github-summary.md");
        using var runId = new EnvironmentVariableScope("GITHUB_RUN_ID", "42");
        using var repository = new EnvironmentVariableScope("GITHUB_REPOSITORY", "owner/repository");
        using var githubOutput = new EnvironmentVariableScope("GITHUB_OUTPUT", githubOutputPath);
        using var githubSummary = new EnvironmentVariableScope("GITHUB_STEP_SUMMARY", githubSummaryPath);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["automation", "plan-github", "--root", workspace.RootPath],
            output,
            error,
            workspace.RootPath,
            httpClient,
            RoadmapAutomationTestGit.Create(),
            TestContext.Current.CancellationToken);
        var workflowOutputs = await File.ReadAllTextAsync(githubOutputPath, TestContext.Current.CancellationToken);
        var workflowSummary = await File.ReadAllTextAsync(githubSummaryPath, TestContext.Current.CancellationToken);
        var plan = RoadmapAutomationPlan.Load(
            RoadmapAutomationService.GetPlanPath(workspace.RootPath),
            workflowOutputs.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Single(line => line.StartsWith("plan_digest=", StringComparison.Ordinal))["plan_digest=".Length..]);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBe(string.Empty);
        workflowOutputs.ShouldContain($"plan_digest={plan.PlanDigest}", StringComparison.Ordinal);
        workflowOutputs.ShouldContain("has_changes=true", StringComparison.Ordinal);
        workflowOutputs.ShouldContain($"branch_name={plan.BranchName}", StringComparison.Ordinal);
        workflowOutputs.ShouldContain($"base_sha={RoadmapAutomationTestPlan.BaseSha}", StringComparison.Ordinal);
        workflowOutputs.ShouldNotContain("untrusted", StringComparison.OrdinalIgnoreCase);
        workflowSummary.ShouldContain("## Roadmap maintenance plan", StringComparison.Ordinal);
        workflowSummary.ShouldNotContain("untrusted", StringComparison.OrdinalIgnoreCase);
    }
}
