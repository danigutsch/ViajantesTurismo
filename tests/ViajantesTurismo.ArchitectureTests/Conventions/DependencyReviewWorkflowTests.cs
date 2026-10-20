using SharedKernel.Testing;
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
    public void License_metadata_exception_is_version_scoped_and_preserves_other_security_checks()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "dependency-review.yml");

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldContain("allow-dependencies-licenses: pkg:nuget/StreamJsonRpc@2.25.29", StringComparison.Ordinal);
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
