using System.Text.RegularExpressions;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RoadmapMaintenanceWorkflowTests
{
    [Fact]
    public void Workflow_is_manual_only_with_one_fixed_boolean_input()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("'on':\n  workflow_dispatch:\n    inputs:\n      apply_changes:", StringComparison.Ordinal);
        workflow.ShouldContain("        type: boolean", StringComparison.Ordinal);
        workflow.ShouldContain("        default: false", StringComparison.Ordinal);
        workflow.ShouldNotContain("pull_request:", StringComparison.Ordinal);
        workflow.ShouldNotContain("push:", StringComparison.Ordinal);
        workflow.ShouldNotContain("schedule:", StringComparison.Ordinal);
        workflow.ShouldNotContain("workflow_call:", StringComparison.Ordinal);
        Regex.Count(workflow, "^      [a-z_]+:$", RegexOptions.Multiline, TimeSpan.FromSeconds(1)).ShouldBe(1);
    }

    [Fact]
    public void Workflow_uses_fixed_concurrency_and_read_only_job_permissions()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("concurrency:\n  group: roadmap-maintenance\n  cancel-in-progress: false", StringComparison.Ordinal);
        workflow.ShouldContain("permissions:\n  contents: read", StringComparison.Ordinal);
        Regex.Count(workflow, "^\\s*issues: read\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1)).ShouldBe(2);
        Regex.Count(workflow, "^\\s*contents: write\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1)).ShouldBe(0);
        Regex.Count(workflow, "^\\s*pull-requests: write\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1)).ShouldBe(0);
        Regex.Count(workflow, "^\\s*issues: write\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1)).ShouldBe(0);
    }

    [Fact]
    public void Apply_requires_a_nonempty_plan_and_the_protected_environment()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("if: ${{ inputs.apply_changes && needs.plan.outputs.has_changes == 'true' }}", StringComparison.Ordinal);
        workflow.ShouldContain("environment: roadmap-maintenance", StringComparison.Ordinal);
        workflow.ShouldContain("if: ${{ steps.seal.outputs.has_changes == 'true' }}", StringComparison.Ordinal);
        workflow.ShouldContain("if: ${{ steps.create_pr.outputs.pull-request-url != '' }}", StringComparison.Ordinal);
    }

    [Fact]
    public void External_actions_are_pinned_to_reviewed_full_commit_shas()
    {
        // Arrange
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;
        var references = Regex.Matches(
                workflow,
                "uses: ([^./\\s][^@\\s]*)@([^\\s]+)\\s+#\\s+([^\\r\\n]+)",
                RegexOptions.None,
                TimeSpan.FromSeconds(1))
            .Select(match => (Action: match.Groups[1].Value, Revision: match.Groups[2].Value, Release: match.Groups[3].Value))
            .ToArray();

        // Act
        var actions = references.Select(reference => reference.Action).ToArray();

        // Assert
        actions.ShouldBe([
            "actions/checkout",
            "actions/upload-artifact",
            "actions/checkout",
            "actions/download-artifact",
            "actions/create-github-app-token",
            "peter-evans/create-pull-request"]);
        foreach (var reference in references)
        {
            reference.Revision.ShouldMatch("^[0-9a-f]{40}$");
            reference.Release.ShouldMatch("^v[0-9]+(?:\\.[0-9]+){0,2}$");
        }

        workflow.ShouldNotContain("uses: actions/checkout@v", StringComparison.Ordinal);
        workflow.ShouldNotContain("uses: actions/create-github-app-token@v", StringComparison.Ordinal);
        workflow.ShouldNotContain("uses: peter-evans/create-pull-request@v", StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_is_integrity_bound_and_uploads_only_the_plan_with_minimal_retention()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("automation plan-github", StringComparison.Ordinal);
        workflow.ShouldContain("roadmap-plan.json", StringComparison.Ordinal);
        workflow.ShouldContain("artifact_digest: ${{ steps.upload_plan.outputs.artifact-digest }}", StringComparison.Ordinal);
        workflow.ShouldContain("retention-days: 1", StringComparison.Ordinal);
        workflow.ShouldContain("path: artifacts/roadmap-maintenance/roadmap-plan.json", StringComparison.Ordinal);
        workflow.ShouldNotContain("path: .", StringComparison.Ordinal);
        workflow.ShouldNotContain("issue bodies", StringComparison.OrdinalIgnoreCase);
        workflow.ShouldNotContain("comments.json", StringComparison.OrdinalIgnoreCase);
        workflow.ShouldNotContain("http", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_revalidates_plan_and_live_inputs_before_writes_and_push()
    {
        // Arrange
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Act
        var downloadIndex = workflow.IndexOf("actions/download-artifact@", StringComparison.Ordinal);
        var validateIndex = workflow.IndexOf("automation validate-plan", StringComparison.Ordinal);
        var applyIndex = workflow.IndexOf("automation apply-plan", StringComparison.Ordinal);
        var sealIndex = workflow.IndexOf("automation seal-result", StringComparison.Ordinal);
        var verifyIndex = workflow.IndexOf("automation verify-result", StringComparison.Ordinal);
        var pullRequestIndex = workflow.IndexOf("peter-evans/create-pull-request@", StringComparison.Ordinal);

        // Assert
        workflow.ShouldContain("ROADMAP_PLAN_DIGEST: ${{ needs.plan.outputs.plan_digest }}", StringComparison.Ordinal);
        workflow.ShouldContain("ROADMAP_ARTIFACT_DIGEST: ${{ needs.plan.outputs.artifact_digest }}", StringComparison.Ordinal);
        workflow.ShouldContain("ROADMAP_RESULT_DIGEST: ${{ steps.seal.outputs.result_digest }}", StringComparison.Ordinal);
        workflow.ShouldContain("digest-mismatch: error", StringComparison.Ordinal);
        downloadIndex.ShouldBeGreaterThan(-1);
        validateIndex.ShouldBeGreaterThan(downloadIndex);
        applyIndex.ShouldBeGreaterThan(validateIndex);
        sealIndex.ShouldBeGreaterThan(applyIndex);
        verifyIndex.ShouldBeGreaterThan(sealIndex);
        pullRequestIndex.ShouldBeGreaterThan(verifyIndex);
    }

    [Fact]
    public void Apply_runs_the_reviewed_local_roadmap_sequence_without_remote_issue_sync()
    {
        // Arrange
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Act
        var reconcileIndex = workflow.IndexOf("reconcile github --apply", StringComparison.Ordinal);
        var previewIndex = workflow.IndexOf("intake github --dry-run", StringComparison.Ordinal);
        var intakeIndex = workflow.IndexOf("intake github --apply", StringComparison.Ordinal);
        var verifyIndex = workflow.IndexOf("-- verify", StringComparison.Ordinal);
        var diffIndex = workflow.IndexOf("-- diff", StringComparison.Ordinal);
        var sealIndex = workflow.IndexOf("automation seal-result", StringComparison.Ordinal);

        // Assert
        reconcileIndex.ShouldBeGreaterThan(-1);
        previewIndex.ShouldBeGreaterThan(reconcileIndex);
        intakeIndex.ShouldBeGreaterThan(previewIndex);
        verifyIndex.ShouldBeGreaterThan(intakeIndex);
        diffIndex.ShouldBeGreaterThan(verifyIndex);
        sealIndex.ShouldBeGreaterThan(diffIndex);
        workflow.ShouldContain("bash scripts/lint-json.sh roadmap", StringComparison.Ordinal);
        workflow.ShouldContain("bash scripts/lint-markdown.sh roadmap/README.md", StringComparison.Ordinal);
        workflow.ShouldContain("bash scripts/lint-links.sh roadmap/README.md", StringComparison.Ordinal);
        workflow.ShouldNotContain("sync github --apply", StringComparison.Ordinal);
    }

    [Fact]
    public void App_token_is_minted_late_and_never_persisted_by_checkout()
    {
        // Arrange
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Act
        var sealIndex = workflow.IndexOf("automation seal-result", StringComparison.Ordinal);
        var tokenIndex = workflow.IndexOf("actions/create-github-app-token@", StringComparison.Ordinal);

        // Assert
        Regex.Count(workflow, "persist-credentials: false", RegexOptions.None, TimeSpan.FromSeconds(1)).ShouldBe(2);
        tokenIndex.ShouldBeGreaterThan(sealIndex);
        workflow.ShouldContain("client-id: ${{ vars.ROADMAP_MAINTENANCE_APP_CLIENT_ID }}", StringComparison.Ordinal);
        workflow.ShouldContain("private-key: ${{ secrets.ROADMAP_MAINTENANCE_APP_PRIVATE_KEY }}", StringComparison.Ordinal);
        workflow.ShouldContain("permission-contents: write", StringComparison.Ordinal);
        workflow.ShouldContain("permission-pull-requests: write", StringComparison.Ordinal);
        workflow.ShouldContain("permission-issues: read", StringComparison.Ordinal);
        Regex.Count(workflow, "\\bPAT\\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).ShouldBe(0);
        workflow.ShouldNotContain("github.token }}\n          branch-token", StringComparison.Ordinal);
    }

    [Fact]
    public void Read_only_workflow_token_is_scoped_only_to_live_remote_steps()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;
        var stepTokenCount = Regex.Count(
            workflow,
            "^          GITHUB_TOKEN: \\$\\{\\{ github\\.token \\}\\}$",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        // Assert
        workflow.ShouldNotContain("    env:\n      GITHUB_TOKEN: ${{ github.token }}", StringComparison.Ordinal);
        stepTokenCount.ShouldBe(7);
    }

    [Fact]
    public void Pull_request_uses_the_app_token_and_a_trusted_branch_without_writing_main()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("automation ensure-branch-available", StringComparison.Ordinal);
        workflow.ShouldContain("token: ${{ steps.app_token.outputs.token }}", StringComparison.Ordinal);
        workflow.ShouldContain("branch: ${{ needs.plan.outputs.branch_name }}", StringComparison.Ordinal);
        workflow.ShouldContain("base: main", StringComparison.Ordinal);
        workflow.ShouldContain("add-paths: roadmap", StringComparison.Ordinal);
        workflow.ShouldContain("sign-commits: true", StringComparison.Ordinal);
        workflow.ShouldContain("Normal pull_request CI is expected", StringComparison.Ordinal);
        workflow.ShouldNotContain("git push origin main", StringComparison.Ordinal);
        workflow.ShouldNotContain("refs/heads/main:", StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_delegates_path_and_tamper_checks_to_the_repository_tool()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;

        // Assert
        workflow.ShouldContain("automation seal-result", StringComparison.Ordinal);
        workflow.ShouldContain("automation verify-result", StringComparison.Ordinal);
        workflow.ShouldContain("automation ensure-branch-available", StringComparison.Ordinal);
        workflow.ShouldNotContain("git diff --name-only", StringComparison.Ordinal);
        workflow.ShouldNotContain("git status --porcelain", StringComparison.Ordinal);
        Regex.Count(workflow, "\\$\\{\\{ inputs\\.", RegexOptions.None, TimeSpan.FromSeconds(1)).ShouldBe(1);
    }

    [Fact]
    public void Every_privileged_remote_step_is_gated_by_the_sealed_no_diff_result()
    {
        // Act
        var workflow = RoadmapMaintenanceWorkflowTestFile.Content;
        var gateCount = Regex.Count(
            workflow,
            "^        if: \\$\\{\\{ steps\\.seal\\.outputs\\.has_changes == 'true' \\}\\}$",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        // Assert
        gateCount.ShouldBe(4);
    }
}
