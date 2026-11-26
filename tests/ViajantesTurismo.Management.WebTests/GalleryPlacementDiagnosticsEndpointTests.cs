using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestScope(TestScope.Integration)]
public sealed class GalleryPlacementDiagnosticsEndpointTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Downloads_only_allowlisted_placement_and_delivery_metadata_for_admin(bool hasCausation, bool hasTriggeringWake)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var terminalEventId = Guid.CreateVersion7();
        var causationEventId = Guid.CreateVersion7();
        var pendingEventId = Guid.CreateVersion7();
        var causationField = hasCausation ? $"\"causationEventId\":\"{causationEventId:D}\"," : string.Empty;
        const string triggeringField = """
            "triggeringWake":{"recordedAt":"2026-11-24T09:10:45Z","publishedAt":"2026-11-24T09:11:05Z","failedAttempts":3,
                             "lastAttemptAt":"2026-11-24T09:10:50Z","nextAttemptAt":null,"lastError":"synthetic-trigger-error",
                             "storageId":"synthetic-trigger-storage","headers":"synthetic-trigger-headers","payload":"synthetic-trigger-payload"},
            """;
        var sourceBody = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":2,"revision":10,
             "acceptedAt":"2026-11-24T09:10:00Z","terminalAt":"2026-11-24T09:11:00Z","terminalEventId":"{{terminalEventId:D}}",
             {{causationField}}"payload":"synthetic-payload","token":"synthetic-secret",
             "originalWake":{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":"2026-11-24T09:10:40Z","failedAttempts":1,
                             "lastAttemptAt":"2026-11-24T09:10:30Z","nextAttemptAt":null,"lastError":"synthetic-error","storageId":"synthetic-storage"},
             {{(hasTriggeringWake ? triggeringField : string.Empty)}}"relatedPendingWakes":[{"eventId":"{{pendingEventId:D}}","recordedAt":"2026-11-24T09:11:00Z","failedAttempts":2,
                                     "lastAttemptAt":"2026-11-24T09:11:30Z","nextAttemptAt":"2026-11-24T09:13:00Z",
                                     "lastError":"synthetic-pending-error"}]}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(sourceBody, HttpStatusCode.OK));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        var configuration = ManagementAuthenticationTestConfiguration.CreateSettings();
        configuration["Security:ForwardedHeaders:KnownProxies:0"] = "127.0.0.1";
        configuration["https_port"] = "443";
        const string scheme = "GalleryDiagnosticsTest";
        using var keyDirectory = new TemporaryDirectory();
        await using var host = KestrelTestHost.Start<CustomerCreationState>(
            environment: Environments.Development,
            configuration: configuration,
            configureTestServices: services =>
            {
                services.AddAuthentication(options => options.DefaultAuthenticateScheme = scheme)
                    .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(scheme, null);
                services.AddDataProtection().UseEphemeralDataProtectionProvider()
                    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory.Path));
                services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(catalog));
            });
        using var browser = new HttpClient { BaseAddress = host.BaseAddress };
        browser.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await browser.GetAsync(new Uri($"/operations/gallery/{tourId:D}/placements/7/diagnostics", UriKind.Relative), ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        var attachment = response.Content.Headers.ContentDisposition.ShouldNotBeNull();
        attachment.DispositionType.ShouldBe("attachment");
        attachment.FileNameStar.ShouldBe($"gallery-placement-{intentId:N}.json");
        content.ShouldNotContain("synthetic-", StringComparison.Ordinal);

        using var export = JsonDocument.Parse(content);
        var placement = export.RootElement;
        placement.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["catalogTourId", "sequence", "intentId", "status", "revision", "acceptedAt", "terminalAt",
             "terminalEventId", "causationEventId", "triggeringWakeEventId", "originalWake", "triggeringWake", "relatedPendingWakes", "timeline"]);
        placement.GetProperty("catalogTourId").GetGuid().ShouldBe(tourId);
        placement.GetProperty("sequence").GetInt64().ShouldBe(7);
        placement.GetProperty("intentId").GetGuid().ShouldBe(intentId);
        placement.GetProperty("status").GetString().ShouldBe("RejectedAfterPublication");
        placement.GetProperty("revision").GetInt64().ShouldBe(10);
        placement.GetProperty("terminalEventId").GetGuid().ShouldBe(terminalEventId);
        placement.GetProperty("triggeringWakeEventId").ValueKind.ShouldBe(JsonValueKind.Null);
        var cause = placement.GetProperty("causationEventId");
        if (hasCausation)
        {
            cause.GetGuid().ShouldBe(causationEventId);
        }
        else
        {
            cause.ValueKind.ShouldBe(JsonValueKind.Null);
        }
        placement.GetProperty("acceptedAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 10, 0, TimeSpan.Zero));
        placement.GetProperty("terminalAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 11, 0, TimeSpan.Zero));
        var original = placement.GetProperty("originalWake");
        original.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["recordedAt", "publishedAt", "failedAttempts", "lastAttemptAt", "nextAttemptAt"]);
        original.GetProperty("failedAttempts").GetInt32().ShouldBe(1);
        var triggering = placement.GetProperty("triggeringWake");
        if (hasCausation && hasTriggeringWake)
        {
            triggering.EnumerateObject().Select(property => property.Name).ShouldBe(
                ["recordedAt", "publishedAt", "failedAttempts", "lastAttemptAt", "nextAttemptAt"]);
            triggering.GetProperty("recordedAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 10, 45, TimeSpan.Zero));
            triggering.GetProperty("publishedAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 11, 5, TimeSpan.Zero));
            triggering.GetProperty("failedAttempts").GetInt32().ShouldBe(3);
            triggering.GetProperty("lastAttemptAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 10, 50, TimeSpan.Zero));
            triggering.GetProperty("nextAttemptAt").ValueKind.ShouldBe(JsonValueKind.Null);
        }
        else
        {
            triggering.ValueKind.ShouldBe(JsonValueKind.Null);
        }
        var pending = placement.GetProperty("relatedPendingWakes").EnumerateArray().ShouldHaveSingleItem();
        pending.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["eventId", "recordedAt", "failedAttempts", "lastAttemptAt", "nextAttemptAt"]);
        pending.GetProperty("eventId").GetGuid().ShouldBe(pendingEventId);
        pending.GetProperty("failedAttempts").GetInt32().ShouldBe(2);
        pending.GetProperty("nextAttemptAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 13, 0, TimeSpan.Zero));

        var acceptedAt = new DateTimeOffset(2026, 11, 24, 9, 10, 0, TimeSpan.Zero);
        List<(string? Kind, DateTimeOffset OccurredAtUtc, Guid? EventId)> expected =
        [
            ("PlacementAccepted", acceptedAt, null),
            ("WakeQueued", acceptedAt, intentId),
            ("WakeLastAttemptRecorded", acceptedAt.AddSeconds(30), intentId),
            ("WakePublicationRecorded", acceptedAt.AddSeconds(40), intentId),
        ];
        if (hasCausation && hasTriggeringWake)
        {
            expected.Add(("WakeQueued", acceptedAt.AddSeconds(45), causationEventId));
            expected.Add(("WakeLastAttemptRecorded", acceptedAt.AddSeconds(50), causationEventId));
        }
        expected.Add(("PlacementTerminal", acceptedAt.AddMinutes(1), terminalEventId));
        expected.Add(("WakeQueued", acceptedAt.AddMinutes(1), pendingEventId));
        if (hasCausation && hasTriggeringWake)
        {
            expected.Add(("WakePublicationRecorded", acceptedAt.AddSeconds(65), causationEventId));
        }
        expected.Add(("WakeLastAttemptRecorded", acceptedAt.AddSeconds(90), pendingEventId));

        var timeline = placement.GetProperty("timeline").EnumerateArray().Select(entry =>
        {
            entry.EnumerateObject().Select(property => property.Name).ShouldBe(["kind", "occurredAtUtc", "eventId"]);
            var eventId = entry.GetProperty("eventId");
            return (entry.GetProperty("kind").GetString(), entry.GetProperty("occurredAtUtc").GetDateTimeOffset(),
                eventId.ValueKind == JsonValueKind.Null ? (Guid?)null : eventId.GetGuid());
        }).ToArray();
        timeline.ShouldBe([.. expected]);

        var request = transport.Requests.ShouldHaveSingleItem().Message;
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe($"/api/v1/catalog/operations/gallery/{tourId:D}/placements/7");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Download_distinguishes_an_expired_verified_trigger_ID_from_unverified_causation(bool hasVerifiedTrigger)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var causationEventId = Guid.CreateVersion7();
        var triggerField = hasVerifiedTrigger ? $"\"triggeringWakeEventId\":\"{causationEventId:D}\"," : string.Empty;
        var source = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":2,
             "revision":null,"acceptedAt":null,"terminalAt":null,"causationEventId":"{{causationEventId:D}}",
             {{triggerField}}"payload":"synthetic-expired-payload","lastError":"synthetic-expired-error"}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(source, HttpStatusCode.OK));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(new Uri($"/operations/gallery/{tourId:D}/placements/7/diagnostics", UriKind.Relative), ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotContain("synthetic-expired-", StringComparison.Ordinal);
        using var export = JsonDocument.Parse(content);
        var placement = export.RootElement;
        placement.GetProperty("causationEventId").GetGuid().ShouldBe(causationEventId);
        Guid? triggerId = placement.TryGetProperty("triggeringWakeEventId", out var trigger)
            && trigger.ValueKind != JsonValueKind.Null ? trigger.GetGuid() : null;
        triggerId.ShouldBe(hasVerifiedTrigger ? causationEventId : null);
        placement.GetProperty("triggeringWake").ValueKind.ShouldBe(JsonValueKind.Null);
        placement.GetProperty("timeline").EnumerateArray().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 1, false)]
    [InlineData(false, 3, true)]
    [InlineData(false, 1, true)]
    public async Task Timeline_uses_only_observed_wake_and_owned_retirement_metadata(bool hasWake, int placementStatus, bool hasRetirement)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var retirementId = Guid.CreateVersion7();
        var retirement = hasRetirement ? $$"""
            ,"retirement":{"retirementId":"{{retirementId:D}}","withdrawnAt":"2026-11-24T10:02:00+01:00",
            "payload":"synthetic-retirement-payload","lastError":"synthetic-retirement-error"}
            """ : string.Empty;
        const string wake = """
            {"recordedAt":"2026-11-24T10:00:00+01:00","publishedAt":null,"failedAttempts":1,
             "lastAttemptAt":"2026-11-24T09:01:00Z","nextAttemptAt":null}
            """;
        var wakeFields = hasWake ? $$"""
            ,"causationEventId":"{{intentId:D}}","originalWake":{{wake}},"triggeringWake":{{wake}},
            "relatedPendingWakes":[{"eventId":"{{intentId:D}}","recordedAt":"2026-11-24T09:00:00Z","failedAttempts":1,
                "lastAttemptAt":"2026-11-24T09:01:00Z","nextAttemptAt":"2026-11-24T09:02:00Z"}]
            """ : string.Empty;
        var source = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":{{placementStatus}},
             "revision":null,"acceptedAt":null,"terminalAt":null{{wakeFields}}{{retirement}}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(source, HttpStatusCode.OK));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(new Uri($"/operations/gallery/{tourId:D}/placements/7/diagnostics", UriKind.Relative), ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var export = JsonDocument.Parse(content);
        var timeline = export.RootElement.GetProperty("timeline").EnumerateArray().ToArray();
        var includesWithdrawal = hasRetirement && placementStatus == 3;
        string[] expectedTimeline = hasWake ? ["WakeQueued", "WakeLastAttemptRecorded"]
            : includesWithdrawal ? ["CatalogWithdrawalRecorded"] : [];
        timeline.Select(entry => entry.GetProperty("kind").GetString()).ShouldBe(expectedTimeline);
        if (hasWake)
        {
            timeline[0].GetProperty("occurredAtUtc").GetDateTimeOffset().Offset.ShouldBe(TimeSpan.Zero);
            timeline.Select(entry => entry.GetProperty("eventId").GetGuid()).ShouldBe([intentId, intentId]);
        }
        if (includesWithdrawal)
        {
            var withdrawal = timeline.ShouldHaveSingleItem();
            var occurredAt = withdrawal.GetProperty("occurredAtUtc").GetDateTimeOffset();
            occurredAt.ShouldBe(new DateTimeOffset(2026, 11, 24, 9, 2, 0, TimeSpan.Zero));
            occurredAt.Offset.ShouldBe(TimeSpan.Zero);
            withdrawal.GetProperty("eventId").GetGuid().ShouldBe(retirementId);
        }
        content.ShouldNotContain("synthetic-retirement-", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Download_denies_callers_without_the_admin_role(string role, HttpStatusCode expected)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        using var transport = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();
        browser.DefaultRequestHeaders.Add("X-Test-Role", role);

        // Act
        using var response = await browser.GetAsync(
            new Uri($"/operations/gallery/{Guid.CreateVersion7():D}/placements/7/diagnostics", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        transport.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway)]
    public async Task Download_preserves_denials_and_uses_safe_upstream_errors(HttpStatusCode upstream, HttpStatusCode expected)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(
            """{"detail":"synthetic-secret"}""", upstream));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(
            new Uri($"/operations/gallery/{Guid.CreateVersion7():D}/placements/7/diagnostics", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        body.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, 7)]
    [InlineData(false, 0)]
    public async Task Download_rejects_an_invalid_selection_before_contacting_catalog(bool missingTour, long sequence)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tourId = missingTour ? Guid.Empty : Guid.CreateVersion7();
        using var transport = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(
            new Uri(FormattableString.Invariant($"/operations/gallery/{tourId:D}/placements/{sequence}/diagnostics"), UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        transport.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, 7, 0)]
    [InlineData(false, 8, 0)]
    [InlineData(false, 7, 99)]
    public async Task Download_rejects_mismatched_or_unknown_placement_metadata(bool wrongTour, long sequence, int status)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var responseTour = wrongTour ? Guid.CreateVersion7() : tourId;
        var source = FormattableString.Invariant($$"""
            {"catalogTourId":"{{responseTour:D}}","sequence":{{sequence}},"intentId":"{{Guid.CreateVersion7():D}}",
             "status":{{status}},"revision":null,"acceptedAt":null,"terminalAt":null}
            """);
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(source, HttpStatusCode.OK));
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(new Uri($"/operations/gallery/{tourId:D}/placements/7/diagnostics", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Unauthorized)]
    [InlineData(false, HttpStatusCode.GatewayTimeout)]
    public async Task Download_uses_a_safe_status_for_token_or_timeout_failures(bool tokenUnavailable, HttpStatusCode expected)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        Exception failure = tokenUnavailable
            ? new UserTokenUnavailableException("synthetic-secret")
            : new TimeoutException("synthetic-secret");
        using var transport = new RecordingHttpMessageHandler(_ => throw failure);
        using var catalog = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, galleryOperationsApi: new CatalogGalleryOperationsApiClient(catalog));
        using var browser = host.GetTestClient();

        // Act
        using var response = await browser.GetAsync(
            new Uri($"/operations/gallery/{Guid.CreateVersion7():D}/placements/7/diagnostics", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        body.ShouldBeEmpty();
    }
}
