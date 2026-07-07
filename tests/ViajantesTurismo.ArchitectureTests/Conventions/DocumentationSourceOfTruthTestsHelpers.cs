namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class DocumentationSourceOfTruthTestsHelpers
{
    public static readonly string[] RequiredSourceOfTruthRows =
    [
        "| Setup and tooling | [README](../README.md#getting-started) | "
            + "[Local tool security](local-tool-security.md), [Dev containers](DEVCONTAINERS.md) |",
        "| Coding standards | [Coding guidelines](CODING_GUIDELINES.md) | "
            + "[.editorconfig](../.editorconfig), [Code quality](CODE_QUALITY.md) |",
        "| Testing | [Test guidelines](TEST_GUIDELINES.md) | "
            + "[Tests README](../tests/README.md), [BDD guide](../tests/BDD_GUIDE.md) |",
        "| Architecture and ADRs | [Architecture overview](architecture/README.md) and "
            + "[Architecture decisions](ARCHITECTURE_DECISIONS.md) | "
            + "[Bounded contexts](bounded-contexts/Admin.md), [Domain aggregates](domain/AGGREGATES.md) |",
        "| Domain validation | [Domain validation](DOMAIN_VALIDATION.md) | "
            + "[Domain aggregates](domain/AGGREGATES.md), [Glossary](domain/GLOSSARY.md) |",
        "| API and client boundaries | [API client boundaries](API_CLIENT_BOUNDARIES.md) | "
            + "[API compatibility](API_COMPATIBILITY.md), [API versioning](API_VERSIONING.md) |",
        "| Configuration and feature flags | [Configuration standards](CONFIGURATION.md) | "
            + "[OpenTelemetry custom telemetry](OPEN_TELEMETRY.md) |",
        "| CI, release, and contribution workflow | [Contributing](../CONTRIBUTING.md) | "
            + "[CI overview](ci/overview.md), [CI governance](ci/governance.md), "
            + "[Pull request template](pull_request_template.md) |"
    ];

    private static readonly string[] GeneratedArchitectureBlocks =
    [
        "project-dependencies",
        "sharedkernel-dependencies",
        "apphost-resources",
        "ci-main-jobs",
        "ci-supplemental-workflows"
    ];

    public static string[] FindGeneratedArchitectureMarkerViolations(string repositoryRoot)
    {
        var architectureDocsPath = Path.Combine(repositoryRoot, "docs", "architecture");
        var architectureDocsText = string.Join(
            Environment.NewLine,
            Directory.GetFiles(architectureDocsPath, "*.md", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));

        return GeneratedArchitectureBlocks
            .SelectMany(block => GeneratedMarkerViolations(block, architectureDocsText))
            .ToArray();
    }

    private static string[] GeneratedMarkerViolations(string block, string architectureDocsText)
    {
        var startMarker = $"<!-- generated:{block}:start -->";
        var endMarker = $"<!-- generated:{block}:end -->";
        var startCount = CountOccurrences(architectureDocsText, startMarker);
        var endCount = CountOccurrences(architectureDocsText, endMarker);

        if (startCount == 1 && endCount == 1)
        {
            return [];
        }

        return [$"{block}: expected one start and one end marker, found {startCount} start and {endCount} end"];
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;

        while (offset < text.Length)
        {
            var index = text.IndexOf(value, offset, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            offset = index + value.Length;
        }

        return count;
    }
}
