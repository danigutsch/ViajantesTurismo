using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.RequiredCheckContractTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class RequiredCheckContractTests
{
    [Fact]
    public void Required_checks_match_workflow_jobs_that_ignore_unrelated_label_events()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var checks = ReadRequiredChecks(Path.Combine(root, "docs", "ci", "governance.md"));

        // Act
        var failures = checks.Select(check => VerifyRequiredCheck(root, check.Name, check.Workflow)).OfType<string>().ToArray();

        // Assert
        checks.Select(static check => check.Name).ToArray().ShouldBe(
            ["Build and Test", "Lint", "Dependency Review", "Secret Scan", "Quality Gate", "PR Metadata"]);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
