using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.SharedKernelToolCallerTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class SharedKernelToolCallerTests
{
    [Fact]
    public async Task Every_sharedkernel_tool_command_line_parses_against_the_pinned_tool()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var commandLines = FindCommandLines(root);

        // Act
        List<string> failures = [];
        foreach (var commandLine in commandLines.Where(static line => line.Command != CommandWithoutDiagramDirective))
        {
            if (await ParseWithDiagramDirective(root, commandLine, TestContext.Current.CancellationToken) is { } failure)
            {
                failures.Add(failure);
            }
        }

        // Assert
        string[] knownCallers =
        [
            ".github/actions/setup-ci-prerequisites/action.yml",
            ".github/workflows/ci.yml",
            ".github/workflows/dependency-review.yml",
            ".github/workflows/devcontainer-smoke.yml",
            ".github/workflows/pr-metadata.yml",
            ".github/workflows/roadmap-maintenance.yml",
            ".github/workflows/sync-labels.yml",
            "opencode.json",
            "setup-dev.ps1",
            "setup-dev.sh",
            "tests/ViajantesTurismo.Admin.ContractTests/AdminOpenApiArtifactDriftGuard.cs",
        ];
        string[] knownCommands =
        [
            "sharedkernel-repo verify-text-encoding",
            "sharedkernel-repo verify-line-endings",
            "sharedkernel-repo verify-json",
            "sharedkernel-repo verify-links",
            "sharedkernel-repo verify-gherkin",
            "sharedkernel-repo github sync-labels",
            "sharedkernel-repo verify-devcontainer",
            "sharedkernel-docs verify",
            "sharedkernel-docs check",
            "sharedkernel-docs generate",
            "sharedkernel-ci select",
            "sharedkernel-ci run",
            "sharedkernel-k6 run",
        ];
        knownCallers.Except(commandLines.Select(static line => line.Source)).ShouldBeEmpty("The scanner missed known callers.");
        knownCommands.Where(command => !commandLines.Any(line => $"{line.Command} {string.Join(' ', line.Arguments)}".StartsWith(command + " ", StringComparison.Ordinal)))
            .ShouldBeEmpty("The scanner missed known commands.");
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
