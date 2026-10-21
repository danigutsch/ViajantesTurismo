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
        workflow.ShouldContain("target: [base, head]", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.head.repo.full_name == github.repository", StringComparison.Ordinal);
        workflow.ShouldContain("persist-credentials: false", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.base.sha", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.head.sha", StringComparison.Ordinal);
        workflow.ShouldContain("dotnet restore ViajantesTurismo.slnx --locked-mode -bl:dependency-restore.binlog", StringComparison.Ordinal);
        workflow.ShouldContain("correlator: viajantes-solution-nuget", StringComparison.Ordinal);
        workflow.ShouldContain("needs: dependency-snapshots", StringComparison.Ordinal);
        workflow.ShouldContain("needs.dependency-snapshots.result", StringComparison.Ordinal);
        workflow.ShouldContain("exit 1", StringComparison.Ordinal);
        workflow.ShouldNotContain("head -20", StringComparison.Ordinal);
        workflow.ShouldNotContain("pull_request_target:", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_stage_the_pinned_candidate_before_locked_restore()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];
        var bootstrapPosition = snapshotJob.IndexOf("uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        var restorePosition = snapshotJob.IndexOf("dotnet restore ViajantesTurismo.slnx --locked-mode -bl:dependency-restore.binlog", StringComparison.Ordinal);

        // Assert
        bootstrapPosition.ShouldBeGreaterThanOrEqualTo(0);
        restorePosition.ShouldBeGreaterThan(bootstrapPosition);
        snapshotJob.ShouldContain("restore-targets: tools/ViajantesTurismo.RepoConfig.Tool/ViajantesTurismo.RepoConfig.Tool.csproj", StringComparison.Ordinal);
        snapshotJob.ShouldContain("restore-local-tools: \"false\"", StringComparison.Ordinal);
        snapshotJob.ShouldContain("trust-dev-certificate: \"false\"", StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_snapshots_support_reviewed_commits_before_the_package_cutover()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var snapshotJob = workflow.Split("  dependency-snapshots:", StringSplitOptions.None)[1]
            .Split("\n  dependency-review:", StringSplitOptions.None)[0];

        // Assert
        snapshotJob.ShouldContain("if: ${{ hashFiles('.config/sharedkernel-candidate.json') != '' }}\n        uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        snapshotJob.ShouldContain("if: ${{ hashFiles('.config/sharedkernel-candidate.json') == '' }}\n        uses: actions/setup-dotnet@c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7", StringComparison.Ordinal);
        snapshotJob.ShouldContain("global-json-file: global.json", StringComparison.Ordinal);
        snapshotJob.ShouldContain("dotnet restore ViajantesTurismo.slnx --locked-mode -bl:dependency-restore.binlog", StringComparison.Ordinal);
    }

    [Fact]
    public void License_review_does_not_use_package_exemptions_that_ignore_versions()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldNotContain("allow-dependencies-licenses:", StringComparison.Ordinal);
        workflow.ShouldContain("fail-on-severity: moderate", StringComparison.Ordinal);
        workflow.ShouldContain("license-check: true", StringComparison.Ordinal);
        workflow.ShouldContain("vulnerability-check: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("pkg:nuget/Magick.NET", StringComparison.Ordinal);
        workflow.ShouldNotContain("warn-only: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("continue-on-error: true", StringComparison.Ordinal);
        workflow.ShouldNotContain("NOASSERTION", StringComparison.Ordinal);
        workflow.ShouldNotContain("MS-PL", StringComparison.Ordinal);
    }
}
