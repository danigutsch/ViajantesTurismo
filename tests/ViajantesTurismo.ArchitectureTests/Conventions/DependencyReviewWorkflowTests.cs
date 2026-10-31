using System.Text.Json;
using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class DependencyReviewWorkflowTests
{
    [Fact]
    public void Dependency_review_requires_complete_base_and_head_snapshots_from_trusted_pull_requests()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldContain("target: ${{ github.event_name == 'pull_request' && fromJSON('[\"base\", \"head\"]') || fromJSON('[\"head\"]') }}", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.head.repo.full_name == github.repository", StringComparison.Ordinal);
        workflow.ShouldContain("github.actor != 'dependabot[bot]'", StringComparison.Ordinal);
        workflow.ShouldContain("correlator: viajantes-solution-nuget", StringComparison.Ordinal);
        workflow.ShouldContain("needs: [plan, dependency-snapshots]", StringComparison.Ordinal);
        workflow.ShouldContain("plan-result: ${{ needs.plan.result }}", StringComparison.Ordinal);
        workflow.ShouldContain("snapshot-result: ${{ needs.dependency-snapshots.result }}", StringComparison.Ordinal);
        workflow.ShouldNotContain("component-detection-dependency-submission-action", StringComparison.Ordinal);
        workflow.ShouldNotContain("pull_request_target:", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_replace_managed_submission_for_main_and_manual_runs()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldContain("push:\n    branches:\n      - main", StringComparison.Ordinal);
        workflow.ShouldContain("workflow_dispatch:", StringComparison.Ordinal);
        workflow.ShouldContain("github.event_name == 'push' || github.event_name == 'workflow_dispatch'", StringComparison.Ordinal);
        workflow.ShouldContain("always() && (github.event_name == 'pull_request' || github.event_name == 'merge_group')", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_call_the_shared_workflow_with_the_only_write_permission()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];

        // Assert
        snapshotJob.ShouldContain("    permissions:\n      contents: write\n", StringComparison.Ordinal);
        snapshotJob.ShouldContain("uses: danigutsch/PandaBear.SharedKernel/.github/workflows/dependency-snapshot-reusable.yml@", StringComparison.Ordinal);
        snapshotJob.ShouldContain("source-repository: danigutsch/PandaBear.SharedKernel", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("secrets:", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("setup-ci-prerequisites", StringComparison.Ordinal);
        workflow.Split("contents: write", StringSplitOptions.None).Length.ShouldBe(2);
    }

    [Fact]
    public void License_review_bounds_preview_approval_before_emitting_the_package_exception()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "workflows", "dependency-review.yml");
        using var candidate = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-candidate.json")));
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-repo.json")));
        var approval = configuration.RootElement.GetProperty("licenseExceptions").GetProperty("approval");

        // Act
        var workflow = File.ReadAllText(path);
        var reviewJob = workflow.Split("\n  dependency-review:", StringSplitOptions.None)[1];

        // Assert
        reviewJob.ShouldContain("uses: danigutsch/PandaBear.SharedKernel/.github/workflows/dependency-review-reusable.yml@", StringComparison.Ordinal);
        reviewJob.ShouldContain("verify-license-exceptions: true", StringComparison.Ordinal);
        reviewJob.ShouldContain("    permissions:\n      contents: read\n", StringComparison.Ordinal);
        reviewJob.ShouldContain("fail-on-severity: moderate", StringComparison.Ordinal);
        reviewJob.ShouldContain("MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, 0BSD, ISC, PostgreSQL, Zlib,", StringComparison.Ordinal);
        reviewJob.ShouldNotContain("secrets:", StringComparison.Ordinal);
        reviewJob.ShouldNotContain("GH_TOKEN", StringComparison.Ordinal);
        approval.GetProperty("file").GetString().ShouldBe(".config/sharedkernel-candidate.json");
        foreach (var value in approval.GetProperty("values").EnumerateObject())
        {
            candidate.RootElement.GetProperty(value.Name).GetRawText().ShouldBe(value.Value.GetRawText());
        }

        workflow.ShouldNotContain("warn-only: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("continue-on-error: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("NOASSERTION", StringComparison.Ordinal);
        workflow.ShouldNotContain("MS-PL", StringComparison.Ordinal);
    }
}
