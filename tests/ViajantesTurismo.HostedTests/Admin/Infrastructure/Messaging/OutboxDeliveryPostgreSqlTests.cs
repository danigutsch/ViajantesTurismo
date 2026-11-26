using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Messaging;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class OutboxDeliveryPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Exact_pending_admin_outbox_row_becomes_a_catalog_transport_delivery_after_relay_startup()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        var command = new CreateTourCommand(
            "OUTBOX-TOUR", "Outbox lifecycle tour", new DateOnly(2028, 5, 8), new DateOnly(2028, 5, 18),
            150m, 20m, 10m, 30m, "EUR", ["Hotel"], 1, 8);
        var eventType = AdminTourCreatedIntegrationEvent.EventType;
        var created = await scenario.CreateTour(command, ct);
        created.IsSuccess.ShouldBeTrue(created.ErrorDetails?.Detail);
        var pendingPage = await scenario.ReadOutboxDelivery(new OutboxDeliveryRequest(null, eventType, null, null, null, 2), ct);
        var queued = pendingPage.Rows.ShouldHaveSingleItem();
        var outboxRequest = new OutboxDeliveryRequest(queued.EventId, eventType, queued.RecordedAt, null, null, 1);
        var transportRequest = new CatalogDeliveryRequest(queued.EventId, eventType, queued.RecordedAt, null, null, 2);

        // Act
        var exactBefore = await scenario.ReadOutboxDelivery(outboxRequest, ct);
        var transportBefore = await scenario.ReadCatalogDelivery(transportRequest, ct);
        var retained = await scenario.RunRelayUntilPublished(outboxRequest, ct);
        var exactAfter = await scenario.ReadOutboxDelivery(outboxRequest, ct);
        var transportAfter = await scenario.ReadCatalogDelivery(transportRequest, ct);

        // Assert
        exactBefore.Rows.ShouldHaveSingleItem().ShouldBe(queued);
        queued.Attempts.ShouldBe(0);
        queued.PublishedAt.ShouldBeNull();
        transportBefore.Rows.ShouldBeEmpty();
        exactAfter.Rows.ShouldBeEmpty();
        var published = retained.Rows.ShouldHaveSingleItem();
        published.StorageId.ShouldBe(queued.StorageId);
        published.EventId.ShouldBe(queued.EventId);
        published.EventType.ShouldBe(queued.EventType);
        published.RecordedAt.ShouldBe(queued.RecordedAt);
        published.PublishedAt.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(queued.RecordedAt);
        var transport = transportAfter.Rows.ShouldHaveSingleItem();
        transport.EventId.ShouldBe(queued.EventId);
        transport.EventType.ShouldBe(queued.EventType);
        transport.Attempts.ShouldBe(0);
        transport.TerminalAt.ShouldBeNull();
    }

    [Fact]
    public async Task Registered_outbox_query_keeps_the_page_limit_and_cursor_for_equal_enqueue_times()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var rows = await OutboxDeliveryRows.Seed(database.ConnectionString, ct);
        var request = new OutboxDeliveryRequest(null, rows.EventType, rows.EnqueuedAt, null, null, 1);

        // Act
        var first = await scenario.ReadOutboxDelivery(request, ct);
        await rows.ReplaceRetryError("System.IO.IOException", ct);
        var sameMetadata = await scenario.ReadOutboxDelivery(request, ct);
        var second = await scenario.ReadOutboxDelivery(request with
        {
            AfterRecordedAt = first.NextRecordedAt,
            AfterStorageId = first.NextStorageId,
        }, ct);

        // Assert
        var retry = first.Rows.ShouldHaveSingleItem();
        retry.StorageId.ShouldBe(rows.Ids[0]);
        retry.Attempts.ShouldBe(2);
        retry.LastAttemptAt.ShouldBe(rows.EnqueuedAt.AddMinutes(1));
        retry.NextAttemptAt.ShouldBe(rows.EnqueuedAt.AddMinutes(2));
        sameMetadata.Rows.ShouldHaveSingleItem().ShouldBe(retry);
        first.NextRecordedAt.ShouldBe(rows.EnqueuedAt);
        first.NextStorageId.ShouldBe(rows.Ids[0]);
        var pending = second.Rows.ShouldHaveSingleItem();
        pending.StorageId.ShouldBe(rows.Ids[1]);
        pending.Attempts.ShouldBe(0);
        pending.NextAttemptAt.ShouldBeNull();
        second.NextRecordedAt.ShouldBeNull();
        second.NextStorageId.ShouldBeNull();
    }
}
