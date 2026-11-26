using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Gallery;

/// <summary>Verifies the bounded Gallery operations response contract.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class CatalogGalleryOperationsEndpointTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Admin_receives_the_retry_count_and_oldest_due_time_at_the_server_snapshot(bool hasDuePlacements)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var observedAt = new DateTimeOffset(2026, 11, 24, 9, 10, 0, TimeSpan.Zero);
        var expected = hasDuePlacements
            ? new GetCatalogGalleryRetryHealthDto(2, observedAt.AddHours(-1))
            : new GetCatalogGalleryRetryHealthDto(0, null);
        var query = new StubCatalogGalleryOperationsQuery(null, expected);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(observedAt));
            services.AddSingleton<ICatalogGalleryOperationsQuery>(query);
        }));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri("/api/v1/catalog/operations/gallery/retry-health", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.RetryHealthRequest.ShouldBe(observedAt);
        var propertyNames = body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        propertyNames.ShouldBe(["duePlacementCount", "oldestDueAt"]);
        body.Deserialize<GetCatalogGalleryRetryHealthDto>(JsonSerializerOptions.Web).ShouldBe(expected);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task Admin_receives_only_the_allowed_placement_and_optional_correlation_metadata_with_no_store(
        bool hasRetainedWake, bool hasTerminalEvent, bool hasCausation)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var bootstrapId = Guid.CreateVersion7();
        var terminalEventId = Guid.CreateVersion7();
        var causationEventId = Guid.CreateVersion7();
        var accepted = new DateTimeOffset(2026, 11, 24, 9, 10, 0, TimeSpan.Zero);
        const long sequence = 7;
        var expected = new GetCatalogGalleryPlacementStatusDto(
            tourId, sequence, intentId, hasTerminalEvent ? CatalogGalleryPlacementStatusDto.Applied : CatalogGalleryPlacementStatusDto.Pending,
            hasTerminalEvent ? 10 : null, accepted, hasTerminalEvent ? accepted.AddMinutes(1) : null)
        {
            TerminalEventId = hasTerminalEvent ? terminalEventId : null,
            CausationEventId = hasCausation ? causationEventId : null,
            TriggeringWakeEventId = hasCausation ? causationEventId : null,
            OriginalWake = hasRetainedWake
                ? new CatalogGalleryOriginalWakeDeliveryDto(accepted, accepted.AddMinutes(2), 1, accepted.AddMinutes(2), null)
                : null,
            TriggeringWake = hasCausation
                ? new CatalogGalleryOriginalWakeDeliveryDto(accepted.AddMinutes(1), accepted.AddMinutes(3), 2, accepted.AddMinutes(3), null)
                : null,
            RelatedPendingWakes = hasRetainedWake
                ? [new CatalogGalleryPendingWakeDeliveryDto(bootstrapId, accepted, 2, accepted.AddMinutes(1), accepted.AddMinutes(3))]
                : null,
        };
        var query = new StubCatalogGalleryOperationsQuery(expected);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri($"/api/v1/catalog/operations/gallery/{tourId:D}/placements/7", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        var payload = await response.Content.ReadFromJsonAsync<GetCatalogGalleryPlacementStatusDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBe((tourId, sequence));
        var placement = payload.ShouldNotBeNull();
        var placementStatus = placement with
        {
            RelatedPendingWakes = null,
        };
        var expectedStatus = expected with
        {
            RelatedPendingWakes = null,
        };
        placementStatus.ShouldBe(expectedStatus);
        List<string> expectedFields = ["acceptedAt", "catalogTourId", "intentId", "revision", "sequence", "status", "terminalAt"];
        if (hasTerminalEvent)
        {
            expectedFields.Add("terminalEventId");
        }

        if (hasCausation)
        {
            expectedFields.AddRange(["causationEventId", "triggeringWake", "triggeringWakeEventId"]);
            json.RootElement.GetProperty("causationEventId").GetGuid().ShouldBe(causationEventId);
            json.RootElement.GetProperty("triggeringWakeEventId").GetGuid().ShouldBe(causationEventId);
            var triggeringFields = json.RootElement.GetProperty("triggeringWake").EnumerateObject()
                .Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
            triggeringFields.ShouldBe(["failedAttempts", "lastAttemptAt", "nextAttemptAt", "publishedAt", "recordedAt"]);
        }

        if (hasRetainedWake)
        {
            expectedFields.AddRange(["originalWake", "relatedPendingWakes"]);
            var wakeFields = json.RootElement.GetProperty("originalWake").EnumerateObject()
                .Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
            wakeFields.ShouldBe(["failedAttempts", "lastAttemptAt", "nextAttemptAt", "publishedAt", "recordedAt"]);
            var pendingWake = json.RootElement.GetProperty("relatedPendingWakes").EnumerateArray().ToArray().ShouldHaveSingleItem();
            var pendingFields = pendingWake.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
            pendingFields.ShouldBe(["eventId", "failedAttempts", "lastAttemptAt", "nextAttemptAt", "recordedAt"]);
            placement.RelatedPendingWakes.ShouldNotBeNull().ShouldHaveSingleItem()
                .ShouldBe(new CatalogGalleryPendingWakeDeliveryDto(bootstrapId, accepted, 2, accepted.AddMinutes(1), accepted.AddMinutes(3)));
        }
        else
        {
            placement.RelatedPendingWakes.ShouldBeNull();
        }

        fields.ShouldBe([.. expectedFields.Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task Missing_placement_returns_not_found_with_no_store()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var query = new StubCatalogGalleryOperationsQuery(null);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri($"/api/v1/catalog/operations/gallery/{tourId:D}/placements/1", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBe((tourId, 1L));
    }
}
