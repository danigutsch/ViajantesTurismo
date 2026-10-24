using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class SecurityWorkflowContractTests
{
    [Fact]
    public async Task Secret_scanning_uses_shared_tooling_without_granting_the_scanner_write_permissions()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflowPath = Path.Combine(root, ".github", "workflows", "secret-scan.yml");

        // Act
        var workflow = await File.ReadAllTextAsync(workflowPath, TestContext.Current.CancellationToken);
        var publication = workflow.IndexOf("\n  upload-secret-scan-sarif:", StringComparison.Ordinal);
        var scanner = workflow[..publication];

        // Assert
        publication.ShouldBeGreaterThan(0);
        scanner.ShouldContain("sharedkernel-repo -- security scan", StringComparison.Ordinal);
        scanner.ShouldContain("setup-security-scanner@", StringComparison.Ordinal);
        scanner.ShouldNotContain("security-events: write", StringComparison.Ordinal);
        scanner.ShouldNotContain("contents: write", StringComparison.Ordinal);
        workflow.ShouldContain("github.event.pull_request.head.repo.full_name == github.repository", StringComparison.Ordinal);
        workflow.ShouldContain("schedule:", StringComparison.Ordinal);
    }
}
