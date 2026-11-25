using System.Text.Json;
using SharedKernel.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.ObservabilityAssetsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Protects the default Logs dashboard from rendering raw log records.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class LogDashboardPrivacyTests
{
    [Fact]
    public void Default_log_rows_use_only_bounded_severity_and_validated_trace_metadata()
    {
        // Arrange
        var projection = """
            | detected_level=~"(?i)warn|warning|error|critical|fatal"
            | keep detected_level, trace_id
            | label_format trace_id=`{{ if and .trace_id (eq (count "(?i)^[0-9a-f]{32}$" .trace_id) 1) }}{{ .trace_id }}{{ end }}`
            | line_format `{{ .detected_level }}`
            | __error__=""
            """;
        var expectedProjection = string.Join(' ', projection.Split('\n', StringSplitOptions.TrimEntries));
        using var dashboard = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot.Locate("ViajantesTurismo.slnx"), "observability", "grafana", "dashboards", "logs.json")));
        var rowQueries = dashboard.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.GetProperty("type").GetString() == "logs")
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray().Select(target => (
                Title: panel.GetProperty("title").GetString(),
                Query: target.GetProperty("expr").GetString())))
            .ToArray();

        // Act
        var unprojected = rowQueries
            .Where(row => row.Query?.EndsWith(expectedProjection, StringComparison.Ordinal) != true)
            .Select(row => row.Title)
            .ToArray();

        // Assert
        rowQueries.ShouldNotBeEmpty();
        unprojected.ShouldBeEmpty("The Logs dashboard should display severity and validated trace correlation for warning-level rows.");
    }

    [Theory]
    [InlineData("""{service_name=~"app.{2}", detected_level="warning"} | line_format `{{ .detected_level }}`""", true)]
    [InlineData("""{service_name=~`app.{2}`} | line_format `{{ .detected_level }}`""", false)]
    public void Selector_matcher_keeps_quoted_regexes_and_excludes_log_templates(string expression, bool forbiddenLabel)
    {
        // Act
        var selectorBodies = StreamSelector().Matches(expression)
            .Select(match => match.Groups["body"].Value)
            .ToArray();
        var containsForbiddenLabel = selectorBodies.Any(body => body.Contains("detected_level", StringComparison.Ordinal));

        // Assert
        containsForbiddenLabel.ShouldBe(forbiddenLabel);
        selectorBodies.ShouldHaveSingleItem();
    }
}
