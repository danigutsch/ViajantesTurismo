using System.Globalization;
using System.Text;
using Reqnroll;

namespace ViajantesTurismo.Admin.BehaviorTests.Hooks;

[Binding]
public class InvariantCoverageHooks(ScenarioContext scenarioContext)
{
    private static readonly InvariantCoverageValidator Validator = new();

    [BeforeScenario(Order = -50)]
    public void TrackInvariantCoverage()
    {
        // Check if scenario has @Invariant tags
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

#pragma warning disable CA1303 // Do not pass literals as localized parameters
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

        Console.WriteLine($"Coverage report written to: {Path.Combine("TestResults", "InvariantCoverage.md")}");
#pragma warning restore CA1303

        var reportPath = Path.Combine("TestResults", "InvariantCoverage.md");
        Directory.CreateDirectory("TestResults");

        File.WriteAllText(reportPath, GenerateMarkdownReport(report));
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
}
