using System.Collections.Concurrent;
using Npgsql;
using SharedKernel.Observability.Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

[Trait(TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(TestTraits.ComponentName, TestTraits.PostgreSqlObservabilityComponent)]
public sealed class PostgreSqlIndexHealthCollectorTests(PostgreSqlIndexHealthCollectorScenario scenario)
    : IClassFixture<PostgreSqlIndexHealthCollectorScenario>
{
    [Fact]
    public async Task Collect_reads_catalog_evidence_without_changing_index_definitions()
    {
        // Arrange
        var definitionsBeforeCollection = await scenario.GetIndexDefinitions(TestContext.Current.CancellationToken);
        var collector = new PostgreSqlIndexHealthCollector(scenario.MonitoringDataSource);

        // Act
        var result = await collector.Collect(TestContext.Current.CancellationToken);
        var definitionsAfterCollection = await scenario.GetIndexDefinitions(TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.ShouldBe(PostgreSqlIndexHealthCollectionOutcome.Collected);
        var expectedDefinitions = definitionsBeforeCollection
            .Select<string, Action<string>>(definition => actual => actual.ShouldBe(definition))
            .ToArray();
        definitionsAfterCollection.ShouldMatchCollection(expectedDefinitions);
    }

    [Fact]
    public async Task Monitoring_role_cannot_create_database_objects()
    {
        // Arrange
        Func<Task> createTable = () => scenario.CreateTableAsMonitoringRole(TestContext.Current.CancellationToken);

        // Act
        var exception = await createTable.ShouldThrow<PostgresException>();

        // Assert
        exception.SqlState.ShouldBe("42501");
    }

    [Fact]
    public async Task Monitoring_role_cannot_create_temporary_database_objects()
    {
        // Arrange
        Func<Task> createTemporaryTable = () => scenario.CreateTemporaryTableAsMonitoringRole(TestContext.Current.CancellationToken);

        // Act
        var exception = await createTemporaryTable.ShouldThrow<PostgresException>();

        // Assert
        exception.SqlState.ShouldBe("42501");
    }

    [Fact]
    public async Task Collect_emits_only_bounded_telemetry_tags()
    {
        // Arrange
        var recordedTags = new ConcurrentQueue<string>();
        using var meterListener = PostgreSqlIndexHealthTelemetryTestListener.Create(recordedTags);
        var collector = new PostgreSqlIndexHealthCollector(scenario.MonitoringDataSource);
        var allowedTags = new[]
        {
            $"{PostgreSqlIndexHealthTelemetry.CollectionMetricName}:outcome=collected",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:action=insufficient_evidence",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:action=review_creation",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:action=review_modification",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=statistics_unavailable",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=statistics_window_too_short",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=table_too_small",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=protected_index",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=unsupported_index_shape",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=per_object_statistics_window_unavailable",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=high_index_read_volume",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=high_sequential_scan_volume",
            $"{PostgreSqlIndexHealthTelemetry.AssessmentMetricName}:reason=insufficient_activity",
        };

        // Act
        _ = await collector.Collect(TestContext.Current.CancellationToken);
        var tags = recordedTags.ToArray();

        // Assert
        tags.ShouldNotBeEmpty();
        foreach (var tag in tags)
        {
            allowedTags.ShouldContain(tag);
        }
    }
}
