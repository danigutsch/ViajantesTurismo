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
            ".github/workflows/release-prep.yml",
            ".github/workflows/roadmap-maintenance.yml",
            ".github/workflows/sync-labels.yml",
            "opencode.json",
            "setup-dev.ps1",
            "setup-dev.sh",
            "tests/ViajantesTurismo.Admin.ContractTests/AdminOpenApiArtifactDriftGuard.cs",
        ];
        string[] knownCommands =
        [
            "repo verify-text-encoding",
            "repo verify-line-endings",
            "repo verify-json",
            "repo verify-links",
            "repo verify-gherkin",
            "repo verify",
            "repo github sync-labels",
            "repo verify-devcontainer",
            "docs verify",
            "docs check",
            "docs generate",
            "ci select",
            "ci run",
            "release calculate",
            "browsermcp serve",
            "loadtest run",
        ];
        knownCallers.Except(commandLines.Select(static line => line.Source)).ShouldBeEmpty("The scanner missed known callers.");
        var parsedCommands = commandLines.Select(static line => $"{line.Command} {string.Join(' ', line.Arguments)}").ToHashSet(StringComparer.Ordinal);
        knownCommands.Where(command => !parsedCommands.Contains(command)
            && !parsedCommands.Any(parsed => parsed.StartsWith(command + " ", StringComparison.Ordinal)))
            .ShouldBeEmpty("The scanner missed known commands.");
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
