using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using SharedKernel.Testing;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class PostgreSqlServerMetricsConfigurationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void The_local_stack_can_opt_into_postgresql_server_metrics(
        bool stackEnabled,
        bool monitoringEnabled,
        bool externalDatabases,
        bool hasOverlay)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration[GrafanaLgtmStackDefaults.EnableObservabilityStackVariable] = stackEnabled ? "1" : "0";
        builder.Configuration["Aspire:Features:PostgreSqlServerMetrics"] = monitoringEnabled ? "true" : "false";
        if (externalDatabases)
        {
            builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "external-admin";
            builder.Configuration[$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "external-catalog";
            builder.Configuration[$"ConnectionStrings:{ResourceNames.SecurityDatabase}"] = "external-security";
        }

        // Act
        builder.AddObservabilityStack(includeDeveloperTooling: true);
        var collector = builder.Resources.OfType<OpenTelemetryCollectorResource>().ShouldHaveSingleItem();
        var mountsOverlay = collector.Annotations.OfType<ContainerMountAnnotation>()
            .Any(mount => Path.GetFileName(mount.Source) == "postgresql-server.yaml");
        var credentialParameters = builder.Resources.OfType<ParameterResource>()
            .Where(parameter => parameter.Name is ResourceNames.PostgreSqlServerMetricsUsername
                or ResourceNames.PostgreSqlServerMetricsPassword).ToArray();

        // Assert
        mountsOverlay.ShouldBe(hasOverlay);
        credentialParameters.Length.ShouldBe(hasOverlay ? 2 : 0);
        credentialParameters.All(static parameter => parameter.Secret).ShouldBeTrue();
    }

    [Fact]
    public void Server_metrics_query_only_aggregate_values_and_export_bounded_labels()
    {
        // Arrange
        var collectorRoot = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "observability", "otel-collector");
        var gateway = File.ReadAllText(Path.Combine(collectorRoot, "config.yaml"));

        // Act
        var path = Path.Combine(collectorRoot, "postgresql-server.yaml");
        var overlayExists = File.Exists(path);

        // Assert
        overlayExists.ShouldBeTrue();
        var overlay = File.ReadAllText(path).ReplaceLineEndings("\n");
        overlay.ShouldContain("collection_interval: 30s", StringComparison.Ordinal);
        overlay.ShouldContain("pg_catalog.pg_stat_database", StringComparison.Ordinal);
        overlay.ShouldContain("max_connections", StringComparison.Ordinal);
        overlay.ShouldContain("metric_name: postgresql.server.connections.used", StringComparison.Ordinal);
        overlay.ShouldContain("metric_name: postgresql.server.connections.limit", StringComparison.Ordinal);
        overlay.ShouldContain("keep_keys(resource.attributes, [])", StringComparison.Ordinal);
        overlay.ShouldContain("keep_keys(attributes, [])", StringComparison.Ordinal);
        overlay.ShouldNotContain("attribute_columns:", StringComparison.Ordinal);
        overlay.ShouldNotContain("pg_stat_activity", StringComparison.Ordinal);
        gateway.ShouldNotContain("sqlquery/postgresql_server", StringComparison.Ordinal);
    }
}
