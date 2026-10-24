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
        workflow.ShouldContain("persist-credentials: false", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.base.sha", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.head.sha", StringComparison.Ordinal);
        workflow.ShouldContain("--correlator viajantes-solution-nuget", StringComparison.Ordinal);
        workflow.ShouldContain("dependency snapshot", StringComparison.Ordinal);
        workflow.ShouldContain("dependency submit --snapshot dependency-input/TestResults/dependency-snapshot.json", StringComparison.Ordinal);
        workflow.ShouldNotContain("component-detection-dependency-submission-action", StringComparison.Ordinal);
        workflow.ShouldContain("needs: [plan, dependency-snapshots]", StringComparison.Ordinal);
        workflow.ShouldContain("test \"$PLAN_RESULT\" = success", StringComparison.Ordinal);
        workflow.ShouldContain("needs.dependency-snapshots.result", StringComparison.Ordinal);
        workflow.ShouldContain("exit 1", StringComparison.Ordinal);
        workflow.ShouldNotContain("head -20", StringComparison.Ordinal);
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
        workflow.ShouldContain("github.event.pull_request.head.sha || github.sha", StringComparison.Ordinal);
        workflow.ShouldContain("|| github.ref }}", StringComparison.Ordinal);
        workflow.ShouldContain("always() && (github.event_name == 'pull_request' || github.event_name == 'merge_group')", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_build_the_generator_from_the_pinned_sharedkernel_source()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];
        var rootCheckout = snapshotJob.IndexOf("ref: ${{ github.event.pull_request.head.sha || github.sha }}", StringComparison.Ordinal);
        var inputCheckout = snapshotJob.IndexOf("path: dependency-input", StringComparison.Ordinal);
        var generatorBuild = snapshotJob.IndexOf("uses: ./.github/actions/build-sharedkernel-tool", StringComparison.Ordinal);
        var generation = snapshotJob.IndexOf("dependency snapshot", StringComparison.Ordinal);

        // Assert
        rootCheckout.ShouldBeGreaterThanOrEqualTo(0);
        inputCheckout.ShouldBeGreaterThan(rootCheckout);
        generatorBuild.ShouldBeGreaterThan(inputCheckout);
        generation.ShouldBeGreaterThan(generatorBuild);
        snapshotJob.ShouldContain("tool: SharedKernel.RepoConfig.Tool", StringComparison.Ordinal);
        snapshotJob.ShouldContain("global-json-file: global.json", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("SNAPSHOT_GENERATOR_REVISION", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain(".local/dependency-review-head", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("setup-ci-prerequisites", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("releases/download", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("sharedkernel-candidate-feed", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_read_historical_locks_without_restoring_historical_packages()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];

        // Assert
        snapshotJob.ShouldContain("path: dependency-input", StringComparison.Ordinal);
        snapshotJob.ShouldContain(
            "dependency snapshot --root dependency-input --sha \"$SNAPSHOT_SHA\" --ref \"$SNAPSHOT_REF\"", StringComparison.Ordinal);
        snapshotJob.ShouldContain(
            "--job \"$SNAPSHOT_JOB_ID\" --correlator viajantes-solution-nuget --output TestResults/dependency-snapshot.json",
            StringComparison.Ordinal);
        snapshotJob.ShouldContain("--repository \"$GITHUB_REPOSITORY\" --force", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("gh api", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("Invoke-RestMethod", StringComparison.Ordinal);
        snapshotJob.Split("GH_TOKEN: ${{ github.token }}", StringSplitOptions.None).Length.ShouldBe(2);
        snapshotJob.Split("GH_TOKEN: ${{ github.token }}", StringSplitOptions.None)[1]
            .Split("      - name:", StringSplitOptions.None)[0]
            .ShouldContain("dependency submit", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("hashFiles('.config/sharedkernel-candidate.json')", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("dotnet restore ViajantesTurismo.slnx", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("component-detection", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("gh release download", StringComparison.Ordinal);
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
        var build = reviewJob.IndexOf("uses: ./.github/actions/build-sharedkernel-tool", StringComparison.Ordinal);
        var verify = reviewJob.IndexOf("dependency verify-license-exceptions --github-output \"$GITHUB_OUTPUT\"", StringComparison.Ordinal);
        var review = reviewJob.IndexOf("uses: actions/dependency-review-action@", StringComparison.Ordinal);

        // Assert
        build.ShouldBeGreaterThan(-1);
        verify.ShouldBeGreaterThan(build);
        review.ShouldBeGreaterThan(verify);
        reviewJob.ShouldContain("timeout-minutes:", StringComparison.Ordinal);
        reviewJob.ShouldContain("id: preview-license", StringComparison.Ordinal);
        reviewJob.ShouldContain("allow-dependencies-licenses: ${{ steps.preview-license.outputs.packages }}", StringComparison.Ordinal);
        reviewJob.ShouldNotContain("GH_TOKEN", StringComparison.Ordinal);
        approval.GetProperty("file").GetString().ShouldBe(".config/sharedkernel-candidate.json");
        foreach (var value in approval.GetProperty("values").EnumerateObject())
        {
            candidate.RootElement.GetProperty(value.Name).GetRawText().ShouldBe(value.Value.GetRawText());
        }

        workflow.ShouldContain("fail-on-severity: moderate", StringComparison.Ordinal);
        workflow.ShouldContain("license-check: true", StringComparison.Ordinal);
        workflow.ShouldContain("vulnerability-check: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("warn-only: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("continue-on-error: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("NOASSERTION", StringComparison.Ordinal);
        workflow.ShouldNotContain("MS-PL", StringComparison.Ordinal);
    }
}
