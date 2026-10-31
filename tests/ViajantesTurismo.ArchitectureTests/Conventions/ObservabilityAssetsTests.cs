using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.ObservabilityAssetsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class ObservabilityAssetsTests
{
    [Fact]
    public void Every_surface_has_a_dashboard_whose_uid_matches_its_file_name()
    {
        // Arrange
        var dashboards = LoadDashboards();

        // Act
        var names = dashboards.Keys.Order(StringComparer.Ordinal).ToArray();

        // Assert
        names.ShouldBe(ExpectedDashboards);
        foreach (var (name, document) in dashboards)
        {
            document.RootElement.GetProperty("uid").GetString().ShouldBe(name);
        }
    }

    [Fact]
    public void Every_dashboard_opens_with_a_purpose_audience_and_decision_text_panel()
    {
        // Arrange
        var dashboards = LoadDashboards();

        // Act
        var violations = new List<string>();
        foreach (var (name, document) in dashboards)
        {
            var first = document.RootElement.GetProperty("panels")[0];
            if (first.GetProperty("type").GetString() != "text")
            {
                violations.Add($"{name}: first panel is not text");
                continue;
            }

            var content = first.GetProperty("options").GetProperty("content").GetString() ?? string.Empty;
            violations.AddRange(RequiredHeadings
                .Where(heading => !content.Contains(heading, StringComparison.Ordinal))
                .Select(heading => $"{name}: missing {heading}"));
        }

        // Assert
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Every_data_panel_has_a_description_a_query_and_a_unit()
    {
        // Arrange
        var dashboards = LoadDashboards();

        // Act
        var violations = new List<string>();
        foreach (var (name, panel) in DataPanels(dashboards))
        {
            var title = panel.GetProperty("title").GetString();
            var type = panel.GetProperty("type").GetString();
            if (!panel.TryGetProperty("description", out var description) || string.IsNullOrWhiteSpace(description.GetString()))
            {
                violations.Add($"{name}/{title}: description");
            }

            if (!panel.TryGetProperty("targets", out var targets) || targets.GetArrayLength() == 0)
            {
                violations.Add($"{name}/{title}: targets");
            }

            if (type is not ("logs" or "table") && !panel.GetProperty("fieldConfig").GetProperty("defaults").TryGetProperty("unit", out _))
            {
                violations.Add($"{name}/{title}: unit");
            }
        }

        // Assert
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Dashboards_do_not_group_by_unbounded_dimensions_and_use_unique_panel_ids()
    {
        // Arrange
        var dashboards = LoadDashboards();

        // Act
        var violations = new List<string>();
        foreach (var (name, document) in dashboards)
        {
            var panels = document.RootElement.GetProperty("panels").EnumerateArray().ToArray();
            var ids = panels.Select(panel => panel.GetProperty("id").GetInt32()).ToArray();
            if (ids.Distinct().Count() != ids.Length)
            {
                violations.Add($"{name}: duplicate panel ids");
            }

            violations.AddRange(panels
                .Where(panel => panel.TryGetProperty("targets", out _))
                .SelectMany(panel => panel.GetProperty("targets").EnumerateArray()
                    .Select(target => target.TryGetProperty("expr", out var expr) ? expr.GetString() ?? string.Empty : string.Empty)
                    .Where(expr => ForbiddenGrouping().IsMatch(expr))
                    .Select(expr => $"{name}/{panel.GetProperty("title").GetString()}: {expr}")));
        }

        // Assert
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Every_alert_rule_has_a_summary_a_description_and_a_runbook_section()
    {
        // Arrange
        var rules = File.ReadAllText(Path.Combine(AlertingRoot(), "rules.yaml"));
        var runbooks = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "docs", "OBSERVABILITY_RUNBOOKS.md"))
            .ToUpperInvariant();

        // Act
        var blocks = RuleSeparator().Split(rules).Skip(1).ToArray();
        var violations = new List<string>();
        violations.AddRange(blocks
            .SelectMany(block => RequiredRuleKeys
                .Where(key => !block.Contains(key, StringComparison.Ordinal))
                .Select(key => $"{block.Split('\n')[0]}: missing {key}")));
        violations.AddRange(RunbookAnchor().Matches(rules)
            .Select(match => match.Groups[1].Value)
            .Where(anchor => !runbooks.Contains($"## {anchor.Replace('-', ' ').ToUpperInvariant()}", StringComparison.Ordinal))
            .Select(anchor => $"runbook anchor missing: {anchor}"));

        // Assert
        blocks.Length.ShouldBeGreaterThanOrEqualTo(5);
        string.Join('\n', violations).ShouldBe(string.Empty);
        foreach (var file in AlertingFiles)
        {
            File.Exists(Path.Combine(AlertingRoot(), file)).ShouldBe(true);
        }

        ForbiddenGrouping().IsMatch(rules).ShouldBe(false);
    }

    [Fact]
    public void Catalog_meter_table_lists_exactly_the_meters_the_application_registers()
    {
        // Arrange
        var catalog = File.ReadAllText(CatalogPath());
        var meterTable = catalog.Split("## Meters", 2)[1].Split("## Metrics", 2)[0];

        // Act
        var listed = MeterTableRow().Matches(meterTable).Select(match => match.Groups["meter"].Value).Order(StringComparer.Ordinal).ToArray();
        var registered = RegisteredMeterNames();

        // Assert
        registered.ShouldNotBeEmpty();
        listed.ShouldBe(registered);
    }

    [Fact]
    public void Every_signal_row_in_the_catalog_has_a_verdict()
    {
        // Arrange
        var catalog = File.ReadAllText(CatalogPath());

        // Act
        var rows = catalog.Split("## Metrics", 2)[1].Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal)).ToArray();
        var rowsWithoutVerdict = rows.Where(row => !VerdictCell().IsMatch(row));

        // Assert
        rows.Length.ShouldBeGreaterThan(20);
        string.Join('\n', rowsWithoutVerdict).ShouldBe(string.Empty);
    }

    [Fact]
    public void Every_repository_metric_used_by_a_dashboard_or_alert_is_in_the_catalog()
    {
        // Arrange
        var catalog = File.ReadAllText(CatalogPath()).Replace('.', '_');
        var expressions = DashboardTargets()
            .Where(target => target.DatasourceType == "prometheus")
            .Select(target => (Owner: $"{target.Dashboard}/{target.Panel}", target.Expression))
            .Concat(AlertExpressions().Select(expression => (Owner: "alert", Expression: expression)));

        // Act
        var missing = expressions
            .SelectMany(entry => MetricNames(entry.Expression).Select(name => (entry.Owner, Name: name)))
            .Where(entry => !PlatformMetricNames.Contains(entry.Name)
                && !PlatformMetricPrefixes.Any(prefix => entry.Name.StartsWith(prefix, StringComparison.Ordinal))
                && !catalog.Contains(BaseMetricName(entry.Name), StringComparison.Ordinal))
            .Select(entry => $"{entry.Owner}: {entry.Name}")
            .Distinct(StringComparer.Ordinal);

        // Assert
        string.Join('\n', missing).ShouldBe(string.Empty);
    }
}
