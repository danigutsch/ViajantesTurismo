using System.Text.Json;
using SharedKernel.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class PostgreSqlObservabilityDashboardTests
{
    [Fact]
    public void Dashboard_uses_the_stable_metrics_without_sensitive_dimensions()
    {
        // Arrange
        var dashboardPath = Path.Combine(
            GetRepositoryRoot(),
            "observability",
            "grafana",
            "dashboards",
            "postgresql-observability.json");
        var dashboard = File.ReadAllText(dashboardPath);
        using var document = JsonDocument.Parse(dashboard);
        var expressions = document.RootElement
            .GetProperty("panels")
            .EnumerateArray()
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString() ?? string.Empty)
            .ToArray();

        // Act
        var dashboardUid = document.RootElement.GetProperty("uid").GetString();

        // Assert
        dashboardUid.ShouldBe("postgresql-observability");
        expressions.ShouldContain(expression => expression.Contains(
            "postgresql_index_health_collections_total",
            StringComparison.Ordinal));
        expressions.ShouldContain(expression => expression.Contains(
            "postgresql_index_health_assessments_total",
            StringComparison.Ordinal));
        foreach (var expression in expressions)
        {
            expression.ShouldNotContain("db_client_", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("schema", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("table", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("index_name", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("tenant", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("pool_name", StringComparison.OrdinalIgnoreCase);
        }
    }
}
