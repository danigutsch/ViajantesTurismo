using Microsoft.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Messaging;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class MessagingHealthPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Registered_read_context_reads_pending_outbox_without_counting_published_rows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        var oldest = DateTimeOffset.UnixEpoch.AddHours(2);
        await MessagingHealthOutboxRows.Seed(database.ConnectionString, oldest, ct);

        // Act
        var health = await scenario.ReadPendingOutbox(ct);
        var snapshot = await scenario.Read(ct);

        // Assert
        health.PendingCount.ShouldBe(2);
        health.OldestEnqueuedAt.ShouldBe(oldest);
        snapshot.AdminPendingOutboxCount.ShouldBe(2);
        snapshot.AdminOldestPendingOutboxEnqueuedAt.ShouldBe(oldest);
    }

    [Fact]
    public async Task Newly_migrated_database_reports_no_terminal_catalog_failures()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);

        // Act
        var health = await scenario.Read(ct);

        // Assert
        health.AdminPendingOutboxCount.ShouldBe(0);
        health.AdminOldestPendingOutboxEnqueuedAt.ShouldBeNull();
        health.CatalogTerminalDeliveryFailureCount.ShouldBe(0);
        health.CatalogOldestTerminalFailureAt.ShouldBeNull();
        health.CatalogRetryCount.ShouldBe(0);
        health.CatalogOldestRetryReceivedAt.ShouldBeNull();
        health.CatalogDueRetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task Snapshot_reports_catalog_retry_and_terminal_ages_without_counting_another_consumer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);

        // Act
        GetMessagingHealthDto seeded;
        await using (await MessagingHealthTransportRows.Seed(database.ConnectionString, ct))
        {
            seeded = await scenario.Read(ct);
        }

        var afterRemoval = await scenario.Read(ct);

        // Assert
        seeded.CatalogTerminalDeliveryFailureCount.ShouldBe(2);
        var terminalAge = seeded.ObservedAtUtc - seeded.CatalogOldestTerminalFailureAt.ShouldNotBeNull();
        terminalAge.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMinutes(30));
        terminalAge.ShouldBeLessThan(TimeSpan.FromMinutes(32));
        seeded.CatalogRetryCount.ShouldBe(2);
        seeded.CatalogDueRetryCount.ShouldBe(1);
        var retryAge = seeded.ObservedAtUtc - seeded.CatalogOldestRetryReceivedAt.ShouldNotBeNull();
        retryAge.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMinutes(150));
        retryAge.ShouldBeLessThan(TimeSpan.FromMinutes(152));
        afterRemoval.CatalogTerminalDeliveryFailureCount.ShouldBe(0);
        afterRemoval.CatalogRetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task Registered_read_context_maps_messaging_storage_without_owning_its_migrations()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);

        // Act
        var (readExcludesTransport, writeExcludesTransport, readExcludesOutbox, writeExcludesOutbox, readTracking) =
            await scenario.InspectModels();

        // Assert
        readExcludesTransport.ShouldBe(true);
        writeExcludesTransport.ShouldBe(false);
        readExcludesOutbox.ShouldBe(true);
        writeExcludesOutbox.ShouldBe(false);
        readTracking.ShouldBe(QueryTrackingBehavior.NoTracking);
    }

    [Fact]
    public async Task Canceled_query_reports_cancellation_and_a_fresh_read_still_succeeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var rows = await MessagingHealthTransportRows.Seed(database.ConnectionString, ct);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Func<Task> read = () => scenario.Read(cancellation.Token);

        // Act
        var canceled = await read.ShouldThrowAssignableTo<OperationCanceledException>();
        var health = await scenario.Read(ct);

        // Assert
        canceled.CancellationToken.ShouldBe(cancellation.Token);
        health.CatalogTerminalDeliveryFailureCount.ShouldBe(2);
    }
}
