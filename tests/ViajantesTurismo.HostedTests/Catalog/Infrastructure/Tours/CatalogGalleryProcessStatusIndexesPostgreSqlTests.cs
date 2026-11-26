using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies local Gallery index installation and safe retries.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryProcessStatusIndexesPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Initializes_fresh_storage_and_accepts_matching_indexes_on_retry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await PostgreSqlEventSourcingSchema.Initialize(dataSource, options: null, ct);

        // Act
        await Task.WhenAll(
            CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct),
            CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct));
        var valid = await CatalogGalleryProcessStatusIndexFixture.CountValidIndexes(dataSource, ct);

        // Assert
        valid.ShouldBe(2);
    }

    [Fact]
    public async Task Keeps_the_conflicting_definition_and_rolls_back_the_other_index()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await PostgreSqlEventSourcingSchema.Initialize(dataSource, options: null, ct);
        await using var conflict = dataSource.CreateCommand("""
            CREATE INDEX ix_catalog_events_tour_retirement ON event_sourcing.events (stream_revision)
            """);
        await conflict.ExecuteNonQueryAsync(ct);

        // Act
        Func<Task> initialize = async () => await CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct);
        var failure = await initialize.ShouldThrow<InvalidOperationException>();
        var valid = await CatalogGalleryProcessStatusIndexFixture.CountValidIndexes(dataSource, ct);

        // Assert
        failure.Message.ShouldContain(CatalogGalleryProcessStatusIndexes.RetirementIndexName, StringComparison.Ordinal);
        valid.ShouldBe(1);
    }

    [Fact]
    public async Task Refuses_the_expected_definition_if_a_concurrent_build_left_it_invalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await PostgreSqlEventSourcingSchema.Initialize(dataSource, options: null, ct);
        await using var writer = await dataSource.OpenConnectionAsync(ct);
        await using var blocker = await writer.BeginTransactionAsync(ct);
        await using var hold = new NpgsqlCommand("LOCK TABLE event_sourcing.events IN ROW EXCLUSIVE MODE", writer, blocker);
        await hold.ExecuteNonQueryAsync(ct);
        await using var indexOwner = await dataSource.OpenConnectionAsync(ct);
        await using var setTimeout = new NpgsqlCommand("SET lock_timeout TO '350ms'", indexOwner);
        await setTimeout.ExecuteNonQueryAsync(ct);
        await using var concurrent = new NpgsqlCommand("""
            CREATE INDEX CONCURRENTLY ix_catalog_events_tour_retirement
            ON event_sourcing.events (stream_id, stream_revision)
            INCLUDE (recorded_at_utc)
            WHERE event_type = 'catalog.tours.retired.v1'
            """, indexOwner);
        Func<Task<int>> build = async () => await concurrent.ExecuteNonQueryAsync(ct);
        var interrupted = await build.ShouldThrow<PostgresException>();
        await blocker.RollbackAsync(ct);

        // Act
        Func<Task> initialize = async () => await CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct);
        var failure = await initialize.ShouldThrow<InvalidOperationException>();
        var valid = await CatalogGalleryProcessStatusIndexFixture.CountValidIndexes(dataSource, ct);
        await using var inspect = dataSource.CreateCommand("""
            SELECT indislive AND NOT indisvalid AND NOT indisready
            FROM pg_catalog.pg_index
            WHERE indexrelid = 'event_sourcing.ix_catalog_events_tour_retirement'::regclass
            """);
        var retained = await inspect.ExecuteScalarAsync(ct);

        // Assert
        interrupted.SqlState.ShouldBe(PostgresErrorCodes.LockNotAvailable);
        failure.Message.ShouldContain(CatalogGalleryProcessStatusIndexes.RetirementIndexName, StringComparison.Ordinal);
        valid.ShouldBe(0);
        retained.ShouldBe(true);
    }

    [Fact]
    public async Task Cancels_a_blocked_build_without_partial_indexes_and_can_retry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await PostgreSqlEventSourcingSchema.Initialize(dataSource, options: null, ct);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var blocker = await connection.BeginTransactionAsync(ct);
        await using var hold = new NpgsqlCommand("LOCK TABLE event_sourcing.events IN ROW EXCLUSIVE MODE", connection, blocker);
        await hold.ExecuteNonQueryAsync(ct);
        // Act
        var cancelled = await CatalogGalleryProcessStatusIndexFixture.CancelBlockedBuild(dataSource, ct);
        var beforeRetry = await CatalogGalleryProcessStatusIndexFixture.CountValidIndexes(dataSource, ct);
        await blocker.RollbackAsync(ct);
        await CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct);
        var afterRetry = await CatalogGalleryProcessStatusIndexFixture.CountValidIndexes(dataSource, ct);

        // Assert
        cancelled.ShouldBeTrue();
        beforeRetry.ShouldBe(0);
        afterRetry.ShouldBe(2);
    }
}
