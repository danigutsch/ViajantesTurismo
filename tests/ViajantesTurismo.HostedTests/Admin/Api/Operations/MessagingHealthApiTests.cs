using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Api.Operations;

[TestScope(TestScope.Integration)]
[TestHost(TestHost.Aspire)]
public sealed class MessagingHealthApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Authorized_messaging_health_exposes_only_aggregates_without_caching()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var route = new Uri("/api/v1/operations/messaging", UriKind.Relative);

        // Act
        using var response = await fixture.Client.GetAsync(route, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var document = JsonDocument.Parse(content);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        properties.ShouldBe([
            "adminOldestPendingOutboxEnqueuedAt", "adminPendingOutboxCount",
            "catalogDueRetryCount", "catalogOldestRetryReceivedAt", "catalogOldestTerminalFailureAt",
            "catalogRetryCount", "catalogTerminalDeliveryFailureCount", "observedAtUtc",
        ]);
        document.RootElement.GetProperty("adminPendingOutboxCount").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Catalog_delivery_returns_only_bounded_metadata_for_the_catalog_consumer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string route = "/api/v1/operations/messaging/catalog-delivery?eventType=test.messaging.health&limit=1";
        await using var rows = await fixture.SeedMessagingHealthTransportRows(ct);

        // Act
        using var first = await fixture.Client.GetAsync(new Uri(route, UriKind.Relative), ct);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync(ct));
        var nextRecordedAt = firstJson.RootElement.GetProperty("nextRecordedAt").GetString().ShouldNotBeNull();
        var nextStorageId = firstJson.RootElement.GetProperty("nextStorageId").GetGuid();
        var continuation = $"{route}&afterRecordedAt={Uri.EscapeDataString(nextRecordedAt)}&afterStorageId={nextStorageId:D}";
        using var second = await fixture.Client.GetAsync(new Uri(continuation, UriKind.Relative), ct);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync(ct));
        using var other = await fixture.Client.GetAsync(new Uri($"{route}&eventId={rows.Ids[3]:D}", UriKind.Relative), ct);
        using var otherJson = JsonDocument.Parse(await other.Content.ReadAsStringAsync(ct));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        other.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var firstRow = firstJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        var secondRow = secondJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        firstRow.GetProperty("storageId").GetGuid().ShouldBe(rows.Ids[0]);
        firstRow.GetProperty("terminalAt").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        secondRow.GetProperty("storageId").GetGuid().ShouldBe(rows.Ids[1]);
        nextStorageId.ShouldBe(rows.Ids[0]);
        otherJson.RootElement.GetProperty("rows").GetArrayLength().ShouldBe(0);
        var fields = firstRow.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        fields.ShouldBe(["attempts", "eventId", "eventType", "lastAttemptAt", "nextAttemptAt", "recordedAt", "storageId", "terminalAt"]);
    }
}
