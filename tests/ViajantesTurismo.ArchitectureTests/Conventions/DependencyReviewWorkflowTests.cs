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
        workflow.ShouldContain("dependency-graph/snapshots", StringComparison.Ordinal);
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
    public void Dependency_snapshots_build_the_generator_from_an_independent_reviewed_source_pin()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];
        var generatorBuild = snapshotJob.IndexOf("dotnet build tools/SharedKernel.RepoConfig.Tool", StringComparison.Ordinal);
        var generation = snapshotJob.IndexOf("dependency snapshot", StringComparison.Ordinal);

        // Assert
        generatorBuild.ShouldBeGreaterThanOrEqualTo(0);
        generation.ShouldBeGreaterThan(generatorBuild);
        snapshotJob.ShouldContain("SNAPSHOT_GENERATOR_REVISION: 8895a57934be23177f9979a23620774e5085eb3d", StringComparison.Ordinal);
        snapshotJob.ShouldContain("repository: danigutsch/PandaBear.SharedKernel", StringComparison.Ordinal);
        snapshotJob.ShouldContain("ref: ${{ env.SNAPSHOT_GENERATOR_REVISION }}", StringComparison.Ordinal);
        snapshotJob.ShouldContain("GENERATOR_REVISION: ${{ env.SNAPSHOT_GENERATOR_REVISION }}", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain(".local/dependency-review-head", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain(".config/sharedkernel-candidate.json", StringComparison.Ordinal);
        snapshotJob.ShouldContain("$revision -cne $env:GENERATOR_REVISION -or $dirty", StringComparison.Ordinal);
        snapshotJob.ShouldContain("global-json-file: .local/dependency-library/global.json", StringComparison.Ordinal);
        snapshotJob.ShouldContain("working-directory: .local/dependency-library", StringComparison.Ordinal);
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
        snapshotJob.ShouldContain("--root $root --sha $env:SNAPSHOT_SHA --ref $env:SNAPSHOT_REF", StringComparison.Ordinal);
        snapshotJob.ShouldContain("--job $env:SNAPSHOT_JOB_ID --correlator viajantes-solution-nuget --output $snapshot", StringComparison.Ordinal);
        snapshotJob.ShouldContain("Invoke-RestMethod -Method Post", StringComparison.Ordinal);
        snapshotJob.ShouldContain("https://api.github.com/repos/$env:GITHUB_REPOSITORY/dependency-graph/snapshots", StringComparison.Ordinal);
        snapshotJob.ShouldContain("-InFile $snapshot", StringComparison.Ordinal);
        snapshotJob.ShouldContain("$ErrorActionPreference = 'Stop'", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("gh api", StringComparison.Ordinal);
        snapshotJob.ShouldContain("if ($LASTEXITCODE -ne 0)", StringComparison.Ordinal);
        snapshotJob.ShouldContain("GH_TOKEN: ${{ github.token }}", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("hashFiles('.config/sharedkernel-candidate.json')", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("dotnet restore ViajantesTurismo.slnx", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("component-detection", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("bootstrap-sharedkernel", StringComparison.Ordinal);
        snapshotJob.ShouldNotContain("gh release download", StringComparison.Ordinal);
    }

    [Fact]
    public void License_review_bounds_preview_approval_before_emitting_the_package_exception()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var path = Path.Combine(root, ".github", "workflows", "dependency-review.yml");
        using var candidate = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-candidate.json")));
        var version = candidate.RootElement.GetProperty("packageVersion").GetString();
        var revision = candidate.RootElement.GetProperty("libraryRevision").GetString();

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldContain("id: preview-license", StringComparison.Ordinal);
        workflow.ShouldContain("allow-dependencies-licenses: ${{ steps.preview-license.outputs.packages }}", StringComparison.Ordinal);
        version.ShouldNotBeNull();
        revision.ShouldNotBeNull();
        workflow.ShouldContain($"$pin.packageVersion -cne '{version}'", StringComparison.Ordinal);
        workflow.ShouldContain($"$pin.libraryRevision -cne '{revision}'", StringComparison.Ordinal);
        workflow.ShouldContain("'StreamJsonRpc' = '2.25.29'", StringComparison.Ordinal);
        workflow.ShouldContain("'Magick.NET-Q8-AnyCPU' = '15.0.0'", StringComparison.Ordinal);
        workflow.ShouldContain("$entry.Value.resolved -cne $approved[$entry.Key]", StringComparison.Ordinal);
        workflow.ShouldContain("git ls-files -- '**/packages.lock.json'", StringComparison.Ordinal);
        workflow.ShouldContain("fail-on-severity: moderate", StringComparison.Ordinal);
        workflow.ShouldContain("license-check: true", StringComparison.Ordinal);
        workflow.ShouldContain("vulnerability-check: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("warn-only: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("continue-on-error: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("NOASSERTION", StringComparison.Ordinal);
        workflow.ShouldNotContain("MS-PL", StringComparison.Ordinal);
    }
}
