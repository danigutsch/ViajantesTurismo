using System.Globalization;
using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Api.Operations;

[TestScope(TestScope.Integration)]
[TestHost(TestHost.Aspire)]
public sealed class OutboxDeliveryApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Admin_pages_pending_publication_metadata_in_stable_order_without_exposing_stored_content()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var rows = await fixture.SeedOutboxDeliveryRows(ct);
        var route = $"/api/v1/operations/messaging/outbox?eventType={rows.EventType}";
        var lowerBound = Uri.EscapeDataString(rows.EnqueuedAt.ToString("O", CultureInfo.InvariantCulture));

        // Act
        using var first = await fixture.Client.GetAsync(new Uri($"{route}&recordedSince={lowerBound}&limit=1", UriKind.Relative), ct);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync(ct));
        var cursorTime = firstJson.RootElement.GetProperty("nextRecordedAt").GetString().ShouldNotBeNull();
        var cursorId = firstJson.RootElement.GetProperty("nextStorageId").GetGuid();
        var continuation = $"{route}&recordedSince={lowerBound}&limit=1&afterRecordedAt={Uri.EscapeDataString(cursorTime)}&afterStorageId={cursorId:D}";
        using var second = await fixture.Client.GetAsync(new Uri(continuation, UriKind.Relative), ct);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync(ct));
        using var older = await fixture.Client.GetAsync(new Uri($"{route}&eventId={rows.Ids[2]:D}", UriKind.Relative), ct);
        using var olderJson = JsonDocument.Parse(await older.Content.ReadAsStringAsync(ct));
        using var published = await fixture.Client.GetAsync(new Uri($"{route}&eventId={rows.Ids[3]:D}", UriKind.Relative), ct);
        using var publishedJson = JsonDocument.Parse(await published.Content.ReadAsStringAsync(ct));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        older.StatusCode.ShouldBe(HttpStatusCode.OK);
        published.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var firstRow = firstJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        var secondRow = secondJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        var olderRow = olderJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        var fields = firstRow.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        fields.ShouldBe(["attempts", "eventId", "eventType", "lastAttemptAt", "nextAttemptAt", "recordedAt", "storageId"]);
        cursorId.ShouldBe(rows.Ids[0]);
        firstRow.GetProperty("storageId").GetGuid().ShouldBe(rows.Ids[0]);
        firstRow.GetProperty("eventId").GetString().ShouldBe(rows.Ids[0].ToString("D"));
        firstRow.GetProperty("eventType").GetString().ShouldBe(rows.EventType);
        firstRow.GetProperty("attempts").GetInt32().ShouldBe(2);
        firstRow.GetProperty("recordedAt").GetDateTimeOffset().ShouldBe(rows.EnqueuedAt);
        firstRow.GetProperty("lastAttemptAt").GetDateTimeOffset().ShouldBe(rows.EnqueuedAt.AddMinutes(1));
        firstRow.GetProperty("nextAttemptAt").GetDateTimeOffset().ShouldBe(rows.EnqueuedAt.AddMinutes(2));
        secondRow.GetProperty("storageId").GetGuid().ShouldBe(rows.Ids[1]);
        secondRow.GetProperty("recordedAt").GetDateTimeOffset().ShouldBe(rows.EnqueuedAt);
        secondRow.GetProperty("attempts").GetInt32().ShouldBe(0);
        secondJson.RootElement.GetProperty("nextStorageId").ValueKind.ShouldBe(JsonValueKind.Null);
        olderRow.GetProperty("storageId").GetGuid().ShouldBe(rows.Ids[2]);
        publishedJson.RootElement.GetProperty("rows").GetArrayLength().ShouldBe(0);
        publishedJson.RootElement.TryGetProperty("includesPublished", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Exact_outbox_search_reads_retained_publication_with_bounded_safe_metadata()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var rows = await fixture.SeedOutboxDeliveryRows(ct);
        const string route = "/api/v1/operations/messaging/outbox?includePublished=true&limit=1";
        var publishedEventId = rows.Ids[3];
        var publishedRecordedAt = rows.EnqueuedAt.AddMinutes(1);
        var inclusiveStart = Uri.EscapeDataString(publishedRecordedAt.ToString("O", CultureInfo.InvariantCulture));
        var laterStart = Uri.EscapeDataString(publishedRecordedAt.AddSeconds(1).ToString("O", CultureInfo.InvariantCulture));
        var publishedRoute = $"{route}&eventId={publishedEventId:D}";

        // Act
        using var retained = await fixture.Client.GetAsync(
            new Uri($"{publishedRoute}&eventType={rows.EventType}&recordedSince={inclusiveStart}", UriKind.Relative), ct);
        using var pending = await fixture.Client.GetAsync(new Uri($"{route}&eventId={rows.Ids[0]:D}", UriKind.Relative), ct);
        string[] exclusions =
        [
            $"{publishedRoute}&eventType=another.event.type",
            $"{publishedRoute}&recordedSince={laterStart}",
            $"{route}&eventId={Guid.CreateVersion7():D}",
        ];
        foreach (var path in exclusions)
        {
            using var excluded = await fixture.Client.GetAsync(new Uri(path, UriKind.Relative), ct);
            using var excludedJson = JsonDocument.Parse(await excluded.Content.ReadAsStringAsync(ct));
            excluded.StatusCode.ShouldBe(HttpStatusCode.OK);
            excludedJson.RootElement.GetProperty("rows").GetArrayLength().ShouldBe(0);
            excludedJson.RootElement.GetProperty("includesPublished").GetBoolean().ShouldBeTrue();
        }

        // Assert
        retained.StatusCode.ShouldBe(HttpStatusCode.OK);
        retained.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var retainedJson = JsonDocument.Parse(await retained.Content.ReadAsStringAsync(ct));
        var retainedResult = retainedJson.RootElement;
        var published = retainedResult.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        published.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(["attempts", "eventId", "eventType", "lastAttemptAt", "nextAttemptAt", "publishedAt", "recordedAt", "storageId"]);
        published.GetProperty("eventId").GetString().ShouldBe(publishedEventId.ToString("D"));
        published.GetProperty("recordedAt").GetDateTimeOffset().ShouldBe(publishedRecordedAt);
        published.GetProperty("publishedAt").GetDateTimeOffset().ShouldBe(rows.EnqueuedAt.AddMinutes(2));
        retainedResult.GetProperty("includesPublished").GetBoolean().ShouldBeTrue();
        retainedResult.GetProperty("nextRecordedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        retainedResult.GetProperty("nextStorageId").ValueKind.ShouldBe(JsonValueKind.Null);
        pending.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var pendingJson = JsonDocument.Parse(await pending.Content.ReadAsStringAsync(ct));
        var pendingRow = pendingJson.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        pendingRow.GetProperty("eventId").GetString().ShouldBe(rows.Ids[0].ToString("D"));
        pendingRow.TryGetProperty("publishedAt", out _).ShouldBeFalse();
    }
}
