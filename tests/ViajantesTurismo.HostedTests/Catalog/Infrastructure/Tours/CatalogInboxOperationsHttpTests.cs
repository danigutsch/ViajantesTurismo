using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies authenticated Catalog inbox operations using completed PostgreSQL deliveries.</summary>
[TestScope(TestScope.Integration)]
[TestHost(TestHost.Aspire)]
public sealed class CatalogInboxOperationsHttpTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Admin_reads_only_catalog_duplicate_counts_and_bounded_recent_evidence()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string stack = HostedTestFixture.CatalogInboxOperationsStack;
        var app = await shared.GetApplication(HostedProfile.Admin, stack, ct);
        var authority = await shared.GetIdentityProviderEndpoint(ct);
        var token = await KeycloakConformanceClient.RequestAccessToken(authority, shared.Configuration.ConformanceUserPassword,
            [ApiAudienceNames.Catalog], ct, realm: shared.Realm(stack));
        using var client = app.CreateHttpClient(ResourceNames.CatalogApi);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var connection = await app.GetConnectionString(ResourceNames.CatalogDatabase, ct);
        await using var writer = await CatalogGalleryIntentPostgreSqlScenario.Create(connection, ct);
        var first = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(),
            Guid.CreateVersion7(), "ops-atlas", "Atlas");
        var second = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(),
            Guid.CreateVersion7(), "ops-alps", "Alps");

        // Act
        const string path = "/api/v1/catalog/operations/messaging/inbox";
        using var empty = await client.GetAsync(new Uri(path, UriKind.Relative), ct);
        await writer.Deliver(first, ct);
        await writer.Deliver(second, ct);
        writer.AdvanceBeyondInboxLease();
        await writer.Deliver(first, ct);
        writer.AdvanceBeyondInboxLease();
        await writer.Deliver(second, ct);
        var latestTime = writer.AdvanceBeyondInboxLease();
        await writer.Deliver(second, ct);

        using var summary = await client.GetAsync(new Uri($"{path}?consumer=admin", UriKind.Relative), ct);
        using var newest = await client.GetAsync(new Uri($"{path}/duplicates?limit=1", UriKind.Relative), ct);

        // Assert
        empty.StatusCode.ShouldBe(HttpStatusCode.OK);
        empty.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var emptyBody = JsonDocument.Parse(await empty.Content.ReadAsStringAsync(ct));
        var emptyHealth = emptyBody.RootElement;
        emptyHealth.GetProperty("consumerName").GetString().ShouldBe(IntegrationEventConsumerNames.Catalog);
        emptyHealth.GetProperty("entriesWithDuplicates").GetInt64().ShouldBe(0);
        emptyHealth.GetProperty("duplicateCount").GetInt64().ShouldBe(0);
        emptyHealth.GetProperty("lastObservedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        summary.StatusCode.ShouldBe(HttpStatusCode.OK);
        newest.StatusCode.ShouldBe(HttpStatusCode.OK);
        summary.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        newest.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var summaryBody = JsonDocument.Parse(await summary.Content.ReadAsStringAsync(ct));
        using var newestBody = JsonDocument.Parse(await newest.Content.ReadAsStringAsync(ct));
        var health = summaryBody.RootElement;
        health.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(["consumerName", "duplicateCount", "entriesWithDuplicates", "lastObservedAt"]);
        health.GetProperty("consumerName").GetString().ShouldBe(IntegrationEventConsumerNames.Catalog);
        health.GetProperty("entriesWithDuplicates").GetInt64().ShouldBe(2);
        health.GetProperty("duplicateCount").GetInt64().ShouldBe(3);
        health.GetProperty("lastObservedAt").GetDateTimeOffset().ShouldBe(latestTime);
        var row = newestBody.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(["duplicateCount", "eventId", "lastObservedAt"]);
        row.GetProperty("eventId").GetGuid().ShouldBe(second.EventId);
        row.GetProperty("duplicateCount").GetInt32().ShouldBe(2);

        var continuationAt = newestBody.RootElement.GetProperty("nextObservedAt").GetString().ShouldNotBeNull();
        var continuationEvent = newestBody.RootElement.GetProperty("nextEventId").GetGuid();
        var nextPath = $"{path}/duplicates?limit=1&afterObservedAt={Uri.EscapeDataString(continuationAt)}&afterEventId={continuationEvent:D}";
        using var older = await client.GetAsync(new Uri(nextPath, UriKind.Relative), ct);
        using var olderBody = JsonDocument.Parse(await older.Content.ReadAsStringAsync(ct));
        older.StatusCode.ShouldBe(HttpStatusCode.OK);
        olderBody.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("eventId").GetGuid().ShouldBe(first.EventId);
        olderBody.RootElement.GetProperty("nextEventId").ValueKind.ShouldBe(JsonValueKind.Null);

        using var exact = await client.GetAsync(new Uri($"{path}/duplicates?eventId={first.EventId:D}", UriKind.Relative), ct);
        using var exactBody = JsonDocument.Parse(await exact.Content.ReadAsStringAsync(ct));
        exact.StatusCode.ShouldBe(HttpStatusCode.OK);
        exactBody.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("duplicateCount").GetInt32().ShouldBe(1);

        var observedSince = Uri.EscapeDataString(latestTime.ToOffset(TimeSpan.FromHours(3)).ToString("O", CultureInfo.InvariantCulture));
        using var recent = await client.GetAsync(new Uri($"{path}/duplicates?limit=1&observedSince={observedSince}", UriKind.Relative), ct);
        using var excluded = await client.GetAsync(
            new Uri($"{path}/duplicates?eventId={first.EventId:D}&observedSince={observedSince}", UriKind.Relative), ct);
        using var recentBody = JsonDocument.Parse(await recent.Content.ReadAsStringAsync(ct));
        using var excludedBody = JsonDocument.Parse(await excluded.Content.ReadAsStringAsync(ct));
        recent.StatusCode.ShouldBe(HttpStatusCode.OK);
        recent.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var boundaryRow = recentBody.RootElement.GetProperty("rows").EnumerateArray().ShouldHaveSingleItem();
        boundaryRow.GetProperty("eventId").GetGuid().ShouldBe(second.EventId);
        boundaryRow.GetProperty("lastObservedAt").GetDateTimeOffset().ShouldBe(latestTime);
        recentBody.RootElement.GetProperty("nextEventId").ValueKind.ShouldBe(JsonValueKind.Null);
        excluded.StatusCode.ShouldBe(HttpStatusCode.OK);
        excludedBody.RootElement.GetProperty("rows").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Inbox_inspection_preserves_operations_authority_and_rejects_invalid_paging_inputs()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string stack = HostedTestFixture.CatalogInboxOperationsStack;
        const string path = "/api/v1/catalog/operations/messaging/inbox";
        var app = await shared.GetApplication(HostedProfile.Admin, stack, ct);
        var authority = await shared.GetIdentityProviderEndpoint(ct);
        var adminToken = await KeycloakConformanceClient.RequestAccessToken(authority, shared.Configuration.ConformanceUserPassword,
            [ApiAudienceNames.Catalog], ct, realm: shared.Realm(stack));
        var operatorToken = await KeycloakConformanceClient.RequestAccessToken(authority, shared.Configuration.OperatorConformanceUserPassword,
            [ApiAudienceNames.Catalog], ct, KeycloakConformanceClient.OperatorUsername, shared.Realm(stack));
        using var admin = app.CreateHttpClient(ResourceNames.CatalogApi);
        using var @operator = app.CreateHttpClient(ResourceNames.CatalogApi);
        using var anonymous = app.CreateHttpClient(ResourceNames.CatalogApi);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        @operator.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        // Act
        using var unauthenticated = await anonymous.GetAsync(new Uri(path, UriKind.Relative), ct);
        using var deniedHealth = await @operator.GetAsync(new Uri(path, UriKind.Relative), ct);
        using var denied = await @operator.GetAsync(new Uri($"{path}/duplicates", UriKind.Relative), ct);
        var missingTimestamp = $"afterEventId={Guid.CreateVersion7():D}";
        string[] invalidFilters = ["limit=0", "limit=26", $"eventId={Guid.Empty:D}", $"afterEventId={Guid.Empty:D}", missingTimestamp,
            "observedSince=unparseable"];
        List<(HttpStatusCode Status, bool NoStore)> results = [];
        foreach (var filter in invalidFilters)
        {
            using var invalid = await admin.GetAsync(new Uri($"{path}/duplicates?{filter}", UriKind.Relative), ct);
            results.Add((invalid.StatusCode, invalid.Headers.CacheControl.ShouldNotBeNull().NoStore));
        }

        // Assert
        unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        deniedHealth.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unauthenticated.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        deniedHealth.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        denied.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        results.ToArray().ShouldBe(Enumerable.Repeat((HttpStatusCode.BadRequest, true), invalidFilters.Length).ToArray());
    }
}
