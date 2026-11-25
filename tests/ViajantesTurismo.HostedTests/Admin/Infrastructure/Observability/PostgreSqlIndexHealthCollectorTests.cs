using System.Collections.Concurrent;
using Npgsql;
using SharedKernel.Observability.Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Observability;

[Trait(Api.Infrastructure.TestTraits.ComponentName, Api.Infrastructure.TestTraits.PostgreSqlObservabilityComponent)]
[TestScope(TestScope.Integration)]
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
    public async Task Monitoring_role_cannot_modify_or_drop_application_data()
    {
        // Arrange
        var operations = new[]
        {
            MonitoringRoleDataOperation.Insert,
            MonitoringRoleDataOperation.Update,
            MonitoringRoleDataOperation.Delete,
            MonitoringRoleDataOperation.Drop,
        };

        // Act
        foreach (var operation in operations)
        {
            Func<Task> execute = () => scenario.AttemptDataMutationAsMonitoringRole(operation, TestContext.Current.CancellationToken);
            var exception = await execute.ShouldThrow<PostgresException>();

            // Assert
            exception.SqlState.ShouldBe("42501");
        }

        var payload = await scenario.GetSamplePayload(TestContext.Current.CancellationToken);

        // Assert
        payload.ShouldBe("sample");
    }

    [Fact]
    public async Task Collect_emits_only_bounded_telemetry_tags()
    {
        // Arrange
        var recordedTags = new ConcurrentQueue<string>();
        using var meterListener = PostgreSqlIndexHealthTelemetryTestListener.Create(recordedTags);
        var collector = new PostgreSqlIndexHealthCollector(scenario.MonitoringDataSource);
        const string collection = PostgreSqlIndexHealthTelemetry.CollectionMetricName;
        const string assessment = PostgreSqlIndexHealthTelemetry.AssessmentMetricName;
        const string collectionOutcome = $"{collection}:sharedkernel.index_health.outcome=collected";
        const string collectionDatabase = $"{collection}:db.system.name=postgresql";
        const string assessmentAction = $"{assessment}:sharedkernel.index_health.action=";
        const string assessmentReason = $"{assessment}:sharedkernel.index_health.reason=";
        var allowedTags = new[]
        {
            collectionOutcome,
            collectionDatabase,
            $"{assessment}:db.system.name=postgresql",
            $"{assessmentAction}insufficient_evidence",
            $"{assessmentAction}review_creation",
            $"{assessmentAction}review_modification",
            $"{assessmentReason}statistics_unavailable",
            $"{assessmentReason}statistics_window_too_short",
            $"{assessmentReason}table_too_small",
            $"{assessmentReason}protected_index",
            $"{assessmentReason}unsupported_index_shape",
            $"{assessmentReason}per_object_statistics_window_unavailable",
            $"{assessmentReason}high_index_read_volume",
            $"{assessmentReason}high_sequential_scan_volume",
            $"{assessmentReason}insufficient_activity",
        };

        // Act
        _ = await collector.Collect(TestContext.Current.CancellationToken);
        var tags = recordedTags.ToArray();

        // Assert
        tags.ShouldContain(collectionOutcome);
        tags.ShouldContain(collectionDatabase);
        foreach (var tag in tags)
        {
            allowedTags.ShouldContain(tag);
        }
    }
}
