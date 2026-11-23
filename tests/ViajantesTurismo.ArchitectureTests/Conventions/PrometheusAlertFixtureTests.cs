using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.ObservabilityAssetsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks that the native Prometheus tests exercise the provisioned alerts.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class PrometheusAlertFixtureTests
{
    [Theory]
    [InlineData("ratio-alerts.rules.yml", "http-5xx-ratio", "test:http_5xx_ratio", "Http5xxRatio")]
    [InlineData("ratio-alerts.rules.yml", "catalog-event-errors", "test:catalog_event_errors", "CatalogEventErrors")]
    [InlineData("collector-export-alert.rules.yml", "collector-export-failing", "test:collector_export_failures", "CollectorExportFailing")]
    public void Native_alert_fixture_uses_the_provisioned_expression_threshold_and_hold(string fixtureFile, string uid, string recordName, string alertName)
    {
        // Arrange
        const string thresholdPrefix = "params: [";
        var grafanaRules = File.ReadAllText(Path.Combine(AlertingRoot(), "rules.yaml"));
        var fixture = File.ReadAllLines(Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "observability", "prometheus", "tests", fixtureFile));

        // Act
        var provisioned = RuleSeparator().Split(grafanaRules)
            .ShouldHaveSingleItem(rule => rule.Split('\n', 2)[0].TrimEnd('\r') == uid);
        var provisionedLines = provisioned.Split('\n', StringSplitOptions.TrimEntries);
        var expression = AlertExpressions(provisioned).ShouldHaveSingleItem();
        var threshold = provisionedLines.ShouldHaveSingleItem(line => line.StartsWith(thresholdPrefix, StringComparison.Ordinal))[thresholdPrefix.Length..^1];
        var hold = provisionedLines.ShouldHaveSingleItem(line => line.StartsWith("for: ", StringComparison.Ordinal));
        var record = string.Join('\n', fixture
            .SkipWhile(line => line.Trim() != $"- record: {recordName}")
            .Skip(1)
            .TakeWhile(line => !line.TrimStart().StartsWith("- ", StringComparison.Ordinal)));
        var alert = fixture.Select(line => line.Trim())
            .SkipWhile(line => line != $"- alert: {alertName}").Take(3).ToArray();

        // Assert
        provisionedLines.ShouldContain("type: gt");
        AlertExpressions(record).ShouldHaveSingleItem().ShouldBe(expression);
        alert.ShouldBe([$"- alert: {alertName}", $"expr: {recordName} > {threshold}", hold]);
    }
}
