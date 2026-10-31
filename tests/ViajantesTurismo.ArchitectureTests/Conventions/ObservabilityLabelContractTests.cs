using System.Text.Json;
using System.Text.RegularExpressions;
using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.ObservabilityAssetsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class ObservabilityLabelContractTests
{
    [Fact]
    public void Label_matchers_in_dashboards_and_alerts_use_values_the_telemetry_emits()
    {
        // Arrange
        var expressions = DashboardTargets().Select(target => target.Expression).Concat(AlertExpressions()).ToArray();

        // Act
        var violations = new List<string>();
        foreach (var expression in expressions)
        {
            foreach (Match selector in Selector().Matches(expression))
            {
                foreach (var (metric, label, _, values) in EmittedLabelValues.Where(known => known.Metric == selector.Groups["metric"].Value))
                {
                    foreach (Match matcher in LabelMatcher().Matches(selector.Groups["body"].Value))
                    {
                        if (matcher.Groups["label"].Value != label)
                        {
                            continue;
                        }

                        var unknown = matcher.Groups["value"].Value.Split('|').Except(values, StringComparer.Ordinal);
                        violations.AddRange(unknown.Select(value => $"{metric}{{{label}{matcher.Groups["op"].Value}\"{value}\"}} is never emitted"));
                    }
                }
            }
        }

        // Assert
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Telemetry_sources_emit_every_label_value_the_contract_allows()
    {
        // Arrange
        var root = GetRepositoryRoot();

        // Act
        var missing = EmittedLabelValues
            .SelectMany(known => known.Values.Select(value => (known, value, source: File.ReadAllText(Path.Combine(root, known.Source)))))
            .Where(entry => !entry.source.Contains($"\"{entry.value}\"", StringComparison.Ordinal))
            .Select(entry => $"{entry.known.Metric}: {entry.value} not found in {entry.known.Source}");

        // Assert
        string.Join('\n', missing).ShouldBe(string.Empty);
    }

    [Fact]
    public void Loki_queries_filter_structured_metadata_after_the_stream_selector_without_case_sensitivity()
    {
        // Arrange
        var lokiTargets = DashboardTargets().Where(target => target.DatasourceType == "loki").ToArray();

        // Act
        var violations = new List<string>();
        foreach (var (dashboard, panel, _, expression) in lokiTargets.Where(target => target.Expression.Contains("detected_level", StringComparison.Ordinal)))
        {
            foreach (Match selector in StreamSelector().Matches(expression))
            {
                if (selector.Groups["body"].Value.Contains("detected_level", StringComparison.Ordinal))
                {
                    violations.Add($"{dashboard}/{panel}: detected_level inside the stream selector");
                }
            }

            foreach (Match filter in LevelFilter().Matches(expression))
            {
                if (!filter.Groups["pattern"].Value.StartsWith("(?i)", StringComparison.Ordinal))
                {
                    violations.Add($"{dashboard}/{panel}: detected_level filter is case-sensitive");
                }
            }
        }

        // Assert
        lokiTargets.ShouldNotBeEmpty();
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Every_data_panel_declares_a_known_verify_mode()
    {
        // Arrange
        var dashboards = LoadDashboards();

        // Act
        var violations = DataPanels(dashboards)
            .Where(entry => !entry.Panel.TryGetProperty("verify", out var mode) || !VerifyModes.Contains(mode.GetString()))
            .Select(entry => $"{entry.Dashboard}/{entry.Panel.GetProperty("title").GetString()}");

        // Assert
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Alert_rules_are_the_expected_set_with_valid_queries_and_states()
    {
        // Arrange
        var rules = File.ReadAllText(Path.Combine(AlertingRoot(), "rules.yaml"));
        var blocks = RuleSeparator().Split(rules).Skip(1).ToArray();

        // Act
        var uids = blocks.Select(block => block.Split('\n')[0].Trim()).Order(StringComparer.Ordinal).ToArray();
        var violations = new List<string>();
        foreach (var block in blocks)
        {
            var uid = block.Split('\n')[0].Trim();
            var condition = ConditionRef().Match(block).Groups["ref"].Value;
            if (!block.Contains($"refId: {condition}", StringComparison.Ordinal))
            {
                violations.Add($"{uid}: condition {condition} has no query");
            }

            violations.AddRange(DatasourceUid().Matches(block)
                .Select(match => match.Groups["uid"].Value)
                .Where(datasourceUid => !AlertDatasourceUids.Contains(datasourceUid))
                .Select(datasourceUid => $"{uid}: unknown datasource {datasourceUid}"));

            if (!NoDataState().IsMatch(block) || !NoDataStates.Contains(NoDataState().Match(block).Groups["state"].Value))
            {
                violations.Add($"{uid}: missing or unknown noDataState");
            }

            if (!block.Contains("execErrState:", StringComparison.Ordinal) || !block.Contains("for:", StringComparison.Ordinal))
            {
                violations.Add($"{uid}: missing execErrState or for");
            }
        }

        // Assert
        uids.ShouldBe(ExpectedAlertRules);
        string.Join('\n', violations).ShouldBe(string.Empty);
    }

    [Fact]
    public void Webhook_contact_point_renders_the_notification_template_per_alert()
    {
        // Arrange
        var contactPoints = File.ReadAllText(Path.Combine(AlertingRoot(), "contact-points.yaml"));
        var templates = File.ReadAllText(Path.Combine(AlertingRoot(), "templates.yaml"));

        // Act
        var templateName = DefinedTemplate().Match(templates).Groups["name"].Value;

        // Assert
        templateName.ShouldNotBeEmpty();
        contactPoints.ShouldContain($"template \"{templateName}\"", StringComparison.Ordinal);
        contactPoints.ShouldContain("range .Alerts", StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_documents_select_the_architecture_tests_that_read_them()
    {
        // Arrange
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(GetRepositoryRoot(), ".config", "sharedkernel-ci.json")));

        // Act
        var selecting = config.RootElement.GetProperty("selection").GetProperty("testPaths").EnumerateArray()
            .Where(entry => entry.GetProperty("paths").EnumerateArray().Any(path => path.GetString() == "docs/OBSERVABILITY_*.md"))
            .SelectMany(entry => entry.GetProperty("tests").EnumerateArray().Select(test => test.GetString()))
            .ToArray();

        // Assert
        selecting.ShouldContain("tests/ViajantesTurismo.ArchitectureTests/ViajantesTurismo.ArchitectureTests.csproj");
    }
}
