#pragma warning disable CA1303

using System.Text;
using System.Text.Json;
using ViajantesTurismo.Admin.BehaviorTests.Infrastructure.Coverage;

namespace ViajantesTurismo.Admin.BehaviorTests.Hooks;

[Binding]
public class InvariantCoverageHooks(ScenarioContext scenarioContext)
{
    private static readonly InvariantCoverageValidator Validator = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    [BeforeScenario(Order = -50)]
    public void TrackInvariantCoverage()
    {
        var invariantTags = scenarioContext.ScenarioInfo.Tags
            .Where(tag => tag.StartsWith("Invariant:", StringComparison.Ordinal))
            .Distinct()
            .ToList();

        foreach (var tag in invariantTags)
        {
            var invariantId = tag.Replace("Invariant:", "", StringComparison.Ordinal);
            Validator.RecordScenarioCoverage(invariantId, scenarioContext.ScenarioInfo.Title);
        }
    }

    [AfterTestRun]
    public static void GenerateInvariantCoverageReport()
    {
        var report = Validator.GenerateReport();

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("INVARIANT COVERAGE REPORT");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Total Invariants: {report.TotalInvariants}");
        Console.WriteLine($"Covered: {report.CoveredInvariants} ({report.CoveragePercentage:F1}%)");
        Console.WriteLine($"Uncovered: {report.UncoveredInvariants.Count}");

        if (report.UncoveredInvariants.Count != 0)
        {
            Console.WriteLine("\nUNCOVERED INVARIANTS:");
            foreach (var invariant in report.UncoveredInvariants.OrderBy(i => i, StringComparer.Ordinal))
            {
                Console.WriteLine($"  ❌ {invariant}");
            }
        }

        Console.WriteLine("\nCOVERAGE BY INVARIANT:");
        foreach (var (invariant, scenarios) in report.InvariantToScenarios.OrderBy(kvp => kvp.Key,
                     StringComparer.Ordinal))
        {
            Console.WriteLine($"  ✓ {invariant} ({scenarios.Count} scenarios)");
            foreach (var scenario in scenarios)
            {
                Console.WriteLine($"      - {scenario}");
            }
        }

        Console.WriteLine(new string('=', 80) + "\n");


        Directory.CreateDirectory("TestResults");

        var markdownPath = Path.Combine("TestResults", "InvariantCoverage.md");
        var jsonPath = Path.Combine("TestResults", "InvariantCoverage.json");

        File.WriteAllText(markdownPath, GenerateMarkdownReport(report));
        File.WriteAllText(jsonPath, GenerateJsonReport(report));

        Console.WriteLine("Coverage reports written to:");
        Console.WriteLine($"  - {markdownPath}");
        Console.WriteLine($"  - {jsonPath}");
    }

    private static string GenerateMarkdownReport(CoverageReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Invariant Coverage Report");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Total Invariants:** {report.TotalInvariants}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"- **Covered:** {report.CoveredInvariants} ({report.CoveragePercentage:F1}%)");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Uncovered:** {report.UncoveredInvariants.Count}");
        sb.AppendLine();

        if (report.UncoveredInvariants.Count != 0)
        {
            sb.AppendLine("## ⚠️ Uncovered Invariants");
            sb.AppendLine();
            sb.AppendLine("| Invariant ID | Status |");
            sb.AppendLine("|--------------|--------|");
            foreach (var invariant in report.UncoveredInvariants.OrderBy(i => i, StringComparer.Ordinal))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"| `{invariant}` | ❌ Not Covered |");
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Coverage Details");
        sb.AppendLine();
        sb.AppendLine("| Invariant ID | Scenarios | Count |");
        sb.AppendLine("|--------------|-----------|-------|");
        foreach (var (invariant, scenarios) in report.InvariantToScenarios.OrderBy(kvp => kvp.Key,
                     StringComparer.Ordinal))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| `{invariant}` | {string.Join(", ", scenarios.Select(s => $"`{s}`"))} | {scenarios.Count} |");
        }

        return sb.ToString();
    }

    private static string GenerateJsonReport(CoverageReport report)
    {
        var json = new
        {
            generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            summary = new
            {
                totalInvariants = report.TotalInvariants,
                coveredInvariants = report.CoveredInvariants,
                uncoveredCount = report.UncoveredInvariants.Count,
                coveragePercentage = Math.Round(report.CoveragePercentage, 2)
            },
            uncoveredInvariants = report.UncoveredInvariants.OrderBy(i => i, StringComparer.Ordinal).ToArray(),
            coverage = report.InvariantToScenarios
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => new
                {
                    invariantId = kvp.Key,
                    scenarios = kvp.Value.ToArray(),
                    scenarioCount = kvp.Value.Count
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(json, JsonSerializerOptions);
    }
}
