using System.Text.Json;
using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class PostgreSqlObservabilityDashboardTests
{
    [Fact]
    public void Server_dashboard_shows_only_the_approved_connection_gauges()
    {
        // Arrange
        var dashboardPath = Path.Combine(
            RepositoryRoot.Locate("ViajantesTurismo.slnx"), "observability", "grafana", "dashboards", "postgresql-server-health.json");

        // Act
        var hasDashboard = File.Exists(dashboardPath);

        // Assert
        hasDashboard.ShouldBeTrue("The approved PostgreSQL server gauges need a dedicated provisioned dashboard.");
        using var dashboard = JsonDocument.Parse(File.ReadAllText(dashboardPath));
        dashboard.RootElement.GetProperty("uid").GetString().ShouldBe("postgresql-server-health");
        var dataPanels = dashboard.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.TryGetProperty("targets", out _))
            .ToArray();
        var panel = dataPanels.ShouldHaveSingleItem();
        panel.GetProperty("fieldConfig").GetProperty("defaults").GetProperty("unit").GetString().ShouldBe("short");
        panel.GetProperty("verify").GetString().ShouldBe("idle-ok");
        var expressions = panel.GetProperty("targets").EnumerateArray()
            .Select(target => target.GetProperty("expr").GetString())
            .ToArray();
        expressions.ShouldBe(["max(postgresql_server_connections_used)", "max(postgresql_server_connections_limit)"]);
    }

    [Fact]
    public void Client_dashboard_shows_bounded_pool_usage_and_command_latency()
    {
        // Arrange
        var dashboardPath = Path.Combine(
            RepositoryRoot.Locate("ViajantesTurismo.slnx"), "observability", "grafana", "dashboards", "npgsql-client-pools.json");

        // Act
        using var dashboard = JsonDocument.Parse(File.ReadAllText(dashboardPath));
        var expressions = dashboard.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.TryGetProperty("targets", out _))
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString() ?? string.Empty)
            .ToArray();
        var metrics = expressions.SelectMany(ObservabilityAssetsTestsHelpers.MetricNames)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        metrics.ShouldBe(["db_client_connection_count", "db_client_connection_max", "db_client_operation_duration_seconds_bucket"]);
        expressions.ShouldContain(expression => expression.Contains("db_client_connection_state=\"used\"", StringComparison.Ordinal));
        foreach (var expression in expressions)
        {
            expression.ShouldContain("db_client_connection_pool_name", StringComparison.Ordinal);
            expression.ShouldNotContain("server_address", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("server_port", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("db_namespace", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("table", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("tenant", StringComparison.OrdinalIgnoreCase);
            expression.ShouldNotContain("trace_id", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Dashboard_uses_the_stable_metrics_without_sensitive_dimensions()
    {
        // Arrange
        var dashboardPath = Path.Combine(
            RepositoryRoot.Locate("ViajantesTurismo.slnx"),
            "observability",
            "grafana",
            "dashboards",
            "postgresql-observability.json");
        var dashboard = File.ReadAllText(dashboardPath);
        using var document = JsonDocument.Parse(dashboard);
        var expressions = document.RootElement
            .GetProperty("panels")
            .EnumerateArray()
            .Where(panel => panel.TryGetProperty("targets", out _))
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString() ?? string.Empty)
            .ToArray();

        // Act
        var dashboardUid = document.RootElement.GetProperty("uid").GetString();

        // Assert
        dashboardUid.ShouldBe("postgresql-observability");
        expressions.ShouldContain(expression => expression.Contains(
            "sharedkernel_index_health_collections_total",
            StringComparison.Ordinal));
        expressions.ShouldContain(expression => expression.Contains(
            "sharedkernel_index_health_assessments_total",
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
