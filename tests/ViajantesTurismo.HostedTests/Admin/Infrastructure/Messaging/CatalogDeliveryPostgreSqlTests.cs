using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Messaging;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogDeliveryPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Two_bounded_pages_return_only_catalog_rows_from_the_migrated_transport()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var rows = await MessagingHealthTransportRows.Seed(database.ConnectionString, ct);
        var request = new CatalogDeliveryRequest(null, null, null, null, null, 2);

        // Act
        var first = await scenario.ReadCatalogDelivery(request, ct);
        var second = await scenario.ReadCatalogDelivery(request with
        {
            AfterRecordedAt = first.NextRecordedAt,
            AfterStorageId = first.NextStorageId,
        }, ct);

        // Assert
        first.Rows.Select(row => row.StorageId).ShouldBe([rows.Ids[0], rows.Ids[1]]);
        first.NextRecordedAt.ShouldBe(first.Rows[1].RecordedAt);
        first.NextStorageId.ShouldBe(rows.Ids[1]);
        first.Rows[0].TerminalAt.ShouldNotBeNull();
        second.Rows.Select(row => row.StorageId).ShouldBe([rows.Ids[2], rows.Ids[4]]);
        second.Rows[0].NextAttemptAt.ShouldNotBeNull();
        second.NextRecordedAt.ShouldBeNull();
        second.NextStorageId.ShouldBeNull();
    }

    [Fact]
    public async Task Exact_event_type_identifier_and_age_filters_never_include_another_consumer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await MessagingHealthPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var rows = await MessagingHealthTransportRows.Seed(database.ConnectionString, ct);
        var request = new CatalogDeliveryRequest(rows.Ids[0].ToString(), "test.messaging.health",
            TimeProvider.System.GetUtcNow().AddHours(-1), null, null, 1);

        // Act
        var matching = await scenario.ReadCatalogDelivery(request, ct);
        var otherConsumer = await scenario.ReadCatalogDelivery(request with
        {
            EventId = rows.Ids[3].ToString(),
            RecordedSince = null
        }, ct);

        // Assert
        matching.Rows.Select(row => row.StorageId).ShouldBe([rows.Ids[0]]);
        matching.NextStorageId.ShouldBeNull();
        otherConsumer.Rows.ShouldBeEmpty();
    }
}
