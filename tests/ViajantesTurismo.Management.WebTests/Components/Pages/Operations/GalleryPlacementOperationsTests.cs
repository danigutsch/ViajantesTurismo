using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class GalleryPlacementOperationsTests : BunitContext
{
    private const string DueWakeCandidateText = "Repeated Gallery wake retry is due.";

    public GalleryPlacementOperationsTests()
    {
        var observedAt = new DateTimeOffset(2026, 11, 24, 9, 11, 30, TimeSpan.Zero);
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(observedAt));
    }

    [Fact]
    public void Gallery_placement_viewer_requires_the_admin_role()
    {
        // Arrange
        var pageType = typeof(GalleryPlacementOperations);

        // Act
        var route = pageType.GetCustomAttributes(typeof(RouteAttribute), false).OfType<RouteAttribute>().ShouldHaveSingleItem();
        var authorization = pageType.GetCustomAttributes(typeof(AuthorizeAttribute), false).OfType<AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/operations/gallery");
        authorization.Roles.ShouldBe("Admin");
    }

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, true)]
    public async Task Displays_only_the_selected_placement_and_clears_it_when_the_next_lookup_is_missing(
        bool hasCausation, bool hasTriggeringWake, bool sameWake, bool hasVerifiedTrigger)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var terminalEventId = Guid.CreateVersion7();
        var causationEventId = sameWake ? intentId : Guid.CreateVersion7();
        var bootstrapId = Guid.CreateVersion7();
        var path = $"/api/v1/catalog/operations/gallery/{tourId:D}/placements/";
        var causationField = hasCausation ? $"\"causationEventId\":\"{causationEventId:D}\"," : string.Empty;
        var verifiedTriggerField = hasVerifiedTrigger ? $"\"triggeringWakeEventId\":\"{causationEventId:D}\"," : string.Empty;
        const string triggeringField = """
            "triggeringWake":{"recordedAt":"2026-11-24T09:11:30Z","publishedAt":"2026-11-24T09:11:45Z",
            "failedAttempts":2,"lastAttemptAt":null,"nextAttemptAt":null,"lastError":"synthetic-trigger-error"},
            """;
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":2,"revision":10,
             "acceptedAt":"2026-11-24T09:10:00Z","terminalAt":"2026-11-24T09:11:00Z","terminalEventId":"{{terminalEventId:D}}","payload":"synthetic-payload",
             {{causationField}}{{verifiedTriggerField}}"originalWake":{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":1,"lastAttemptAt":"2026-11-24T09:10:30Z",
                             "nextAttemptAt":"2026-11-24T09:12:00Z","lastError":"synthetic-error","storageId":"synthetic-storage"},
            {{(hasTriggeringWake ? triggeringField : string.Empty)}}"relatedPendingWakes":[{"eventId":"{{bootstrapId:D}}","recordedAt":"2026-11-24T09:11:00Z","failedAttempts":2,
                                     "lastAttemptAt":"2026-11-24T09:11:30Z","nextAttemptAt":"2026-11-24T09:13:00Z",
                                     "lastError":"synthetic-pending-error"}]
            }
            """;
        using var transport = new RecordingHttpMessageHandler(request => request.Message.RequestUri?.AbsolutePath == path + "7"
            ? HttpResponseFactory.Json(body, HttpStatusCode.OK)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));

        // Act
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });
        await page.Find("form").SubmitAsync();
        var placement = page.Find("dl").TextContent;
        var originalWake = page.Find("section[aria-labelledby='gallery-original-wake-heading']").TextContent;
        var triggeringWakes = page.FindAll("section[aria-labelledby='gallery-triggering-wake-heading']")
            .Select(section => section.TextContent).ToArray();
        var pendingWake = page.Find("section[aria-labelledby='gallery-pending-wakes-heading'] tbody tr").TextContent;
        var resultMarkup = page.Markup;
        var downloadPaths = page.FindAll("a[download]").Select(link => link.GetAttribute("href")).ToArray();
        var inboxLinks = page.FindAll("a[href^='operations/messaging/catalog-delivery?view=inbox']")
            .Select(link => link.GetAttribute("href")).ToArray();
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "8" });
        await page.Find("form").SubmitAsync();

        // Assert
        placement.ShouldContain(intentId.ToString("D"), StringComparison.Ordinal);
        placement.ShouldContain("Committed event ID", StringComparison.Ordinal);
        placement.ShouldContain(terminalEventId.ToString("D"), StringComparison.Ordinal);
        placement.ShouldContain("Causation event ID", StringComparison.Ordinal);
        placement.ShouldContain(hasCausation ? causationEventId.ToString("D") : "Unknown", StringComparison.Ordinal);
        placement.ShouldContain("Rejected after publication", StringComparison.Ordinal);
        placement.ShouldContain("2026-11-24 09:10:00Z", StringComparison.Ordinal);
        placement.ShouldContain("2026-11-24 09:11:00Z", StringComparison.Ordinal);
        originalWake.ShouldContain("Original wake delivery", StringComparison.Ordinal);
        originalWake.ShouldContain("Retry scheduled", StringComparison.Ordinal);
        originalWake.ShouldContain("2026-11-24 09:12:00Z", StringComparison.Ordinal);
        if (hasCausation && !sameWake)
        {
            var trigger = triggeringWakes.ShouldHaveSingleItem();
            trigger.ShouldContain("Triggering wake delivery", StringComparison.Ordinal);
            trigger.ShouldContain(hasTriggeringWake ? "2026-11-24 09:11:45Z" : "No retained delivery metadata is available.", StringComparison.Ordinal);
        }
        else
        {
            triggeringWakes.ShouldBeEmpty();
        }
        if (sameWake)
        {
            originalWake.ShouldContain("This wake triggered the committed transition.", StringComparison.Ordinal);
        }
        pendingWake.ShouldContain(bootstrapId.ToString("D"), StringComparison.Ordinal);
        pendingWake.ShouldContain("2026-11-24 09:13:00Z", StringComparison.Ordinal);
        downloadPaths.ShouldBe([$"/operations/gallery/{tourId:D}/placements/7/diagnostics"]);
        var originalInboxLink = $"operations/messaging/catalog-delivery?view=inbox&eventId={intentId:D}#catalog-inbox-heading";
        var triggeringInboxLink = $"operations/messaging/catalog-delivery?view=inbox&eventId={causationEventId:D}#catalog-inbox-heading";
        string[] expectedInboxLinks = !sameWake && (hasTriggeringWake || hasVerifiedTrigger)
            ? [originalInboxLink, triggeringInboxLink] : [originalInboxLink];
        inboxLinks.ShouldBe(expectedInboxLinks);
        resultMarkup.ShouldNotContain("synthetic-payload", StringComparison.Ordinal);
        resultMarkup.ShouldNotContain("synthetic-error", StringComparison.Ordinal);
        resultMarkup.ShouldNotContain("synthetic-storage", StringComparison.Ordinal);
        resultMarkup.ShouldNotContain("synthetic-pending-error", StringComparison.Ordinal);
        resultMarkup.ShouldNotContain("synthetic-trigger-error", StringComparison.Ordinal);
        page.FindAll("dl").ShouldBeEmpty();
        page.FindAll("a[download]").ShouldBeEmpty();
        page.FindAll("a[href^='operations/messaging/catalog-delivery?view=inbox']").ShouldBeEmpty();
        page.FindAll("#gallery-pending-wakes-heading").ShouldBeEmpty();
        page.Find("[role='status']").TextContent.ShouldBe("No Gallery placement matches this tour and sequence.");
        transport.Requests.Select(request => request.Message.RequestUri?.AbsolutePath).ShouldBe([path + "7", path + "8"]);
        transport.Requests.Select(request => request.Message.Method).ShouldBe([HttpMethod.Get, HttpMethod.Get]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Marks_the_original_wake_as_trigger_after_delivery_expiry_only_when_verified(bool hasVerifiedTrigger)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var terminalEventId = Guid.CreateVersion7();
        var verifiedTrigger = hasVerifiedTrigger ? $",\"triggeringWakeEventId\":\"{intentId:D}\"" : string.Empty;
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":1,"revision":10,
             "acceptedAt":"2026-11-24T09:10:00Z","terminalAt":"2026-11-24T09:11:00Z","terminalEventId":"{{terminalEventId:D}}",
             "causationEventId":"{{intentId:D}}"{{verifiedTrigger}}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();

        // Assert
        var originalWake = page.Find("section[aria-labelledby='gallery-original-wake-heading']");
        originalWake.TextContent.ShouldContain("No retained delivery metadata is available.", StringComparison.Ordinal);
        var marksOriginalAsTrigger = originalWake.TextContent.Contains("This wake triggered the committed transition.", StringComparison.Ordinal);
        marksOriginalAsTrigger.ShouldBe(hasVerifiedTrigger);
        var inboxLink = originalWake.QuerySelectorAll("a").ShouldHaveSingleItem();
        inboxLink.GetAttribute("href").ShouldBe($"operations/messaging/catalog-delivery?view=inbox&eventId={intentId:D}#catalog-inbox-heading");
        page.FindAll("#gallery-triggering-wake-heading").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(3, true, true)]
    [InlineData(3, false, false)]
    [InlineData(1, true, false)]
    public async Task Shows_owned_retirement_withdrawal_only_for_a_placement_closed_by_retirement(
        int placementStatus, bool hasRetirement, bool showsWithdrawal)
    {
        // Arrange
        var retirementId = Guid.CreateVersion7();
        var retirement = hasRetirement
            ? $$""","retirement":{"retirementId":"{{retirementId:D}}","withdrawnAt":"2026-11-24T10:12:00+01:00"}"""
            : string.Empty;
        var tourId = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{Guid.CreateVersion7():D}}",
             "status":{{placementStatus}},"revision":null,"acceptedAt":null,"terminalAt":null{{retirement}}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        var detail = page.Find("dl").TextContent;
        var outboxLinks = page.FindAll("a[href^='operations/messaging/outbox?']");

        // Assert
        detail.Contains("Catalog withdrawal recorded (UTC)", StringComparison.Ordinal).ShouldBe(showsWithdrawal);
        detail.Contains(retirementId.ToString("D"), StringComparison.Ordinal).ShouldBe(showsWithdrawal);
        detail.Contains("2026-11-24 09:12:00Z", StringComparison.Ordinal).ShouldBe(showsWithdrawal);
        if (showsWithdrawal)
        {
            outboxLinks.ShouldHaveSingleItem().GetAttribute("href")
                .ShouldBe($"operations/messaging/outbox?view=retained&eventId={retirementId:D}");
        }
        else
        {
            outboxLinks.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traces_the_retirement_event_before_and_after_withdrawal(bool withdrawalRecorded)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var adminEventId = Guid.CreateVersion7();
        var retirementId = Guid.CreateVersion7();
        var withdrawal = withdrawalRecorded
            ? $$""","retirement":{"retirementId":"{{retirementId:D}}","withdrawnAt":"2026-11-24T09:12:00Z"}"""
            : string.Empty;
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{Guid.CreateVersion7():D}}",
             "status":3,"revision":10,"acceptedAt":null,"terminalAt":"2026-11-24T09:11:00Z",
             "terminalEventId":"{{Guid.CreateVersion7():D}}","causationEventId":"{{adminEventId:D}}"{{withdrawal}}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        var outboxLinks = page.FindAll("a[href^='operations/messaging/outbox?']");
        var triggeringWake = page.FindAll("#gallery-triggering-wake-heading");

        // Assert
        outboxLinks.ShouldHaveSingleItem().GetAttribute("href")
            .ShouldBe($"operations/messaging/outbox?view=retained&eventId={adminEventId:D}");
        triggeringWake.ShouldBeEmpty();
        page.Find("dl").TextContent.Contains("Catalog withdrawal recorded (UTC)", StringComparison.Ordinal).ShouldBe(withdrawalRecorded);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Overlapping_lookups_display_only_the_latest_selection(bool olderCompletesFirst, bool olderLosesSession)
    {
        // Arrange
        var olderTourId = Guid.CreateVersion7();
        var latestTourId = Guid.CreateVersion7();
        var olderResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latestResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transport = new DelegateHttpMessageHandler((request, _) => request.RequestUri?.AbsolutePath switch
        {
            var path when path == $"/api/v1/catalog/operations/gallery/{olderTourId:D}/placements/7" => olderResponse.Task,
            var path when path == $"/api/v1/catalog/operations/gallery/{latestTourId:D}/placements/8" => latestResponse.Task,
            _ => throw new InvalidOperationException($"Unexpected Gallery lookup: {request.RequestUri}"),
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        var latestBody = $$"""
            {"catalogTourId":"{{latestTourId:D}}","sequence":8,"intentId":"{{Guid.CreateVersion7():D}}",
             "status":1,"revision":null,"acceptedAt":null,"terminalAt":null}
            """;
        var olderBody = $$"""
            {"catalogTourId":"{{olderTourId:D}}","sequence":7,"intentId":"{{Guid.CreateVersion7():D}}",
             "status":1,"revision":null,"acceptedAt":null,"terminalAt":null}
            """;

        // Act
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = olderTourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });
        var olderLookup = page.Find("form").SubmitAsync();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = latestTourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "8" });
        var latestLookup = page.Find("form").SubmitAsync();
        if (!olderCompletesFirst)
        {
            latestResponse.SetResult(HttpResponseFactory.Json(latestBody, HttpStatusCode.OK));
            await latestLookup;
        }

        if (olderLosesSession)
        {
            olderResponse.SetException(new UserTokenUnavailableException("synthetic-stale-session"));
        }
        else
        {
            olderResponse.SetResult(HttpResponseFactory.Json(olderBody, HttpStatusCode.OK));
        }

        await olderLookup;
        var interimStatuses = page.FindAll("[role='status']");
        var intermediateStatus = interimStatuses.Count == 0 ? null : interimStatuses[0].TextContent;
        var intermediatePlacementCount = page.FindAll("dl").Count;
        if (olderCompletesFirst)
        {
            latestResponse.SetResult(HttpResponseFactory.Json(latestBody, HttpStatusCode.OK));
            await latestLookup;
        }

        // Assert
        if (olderCompletesFirst)
        {
            intermediateStatus.ShouldBe("Loading placement status…");
            intermediatePlacementCount.ShouldBe(0);
        }

        page.FindAll("[role='alert']").ShouldBeEmpty();
        var placement = page.Find("dl").TextContent;
        placement.ShouldContain(latestTourId.ToString("D"), StringComparison.Ordinal);
        placement.ShouldNotContain(olderTourId.ToString("D"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":0,"lastAttemptAt":null,"nextAttemptAt":null}""", "Pending", "")]
    [InlineData("""{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":1,"lastAttemptAt":null,"nextAttemptAt":"2026-11-24T09:11:29Z"}""", "Retry due", "")]
    [InlineData("""{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":1,"lastAttemptAt":null,"nextAttemptAt":"2026-11-24T09:11:30Z"}""", "Retry due", "")]
    [InlineData("""{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":1,"lastAttemptAt":null,"nextAttemptAt":"2026-11-24T09:11:31Z"}""", "Retry scheduled", "")]
    [InlineData(
        """{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":"2026-11-24T09:11:00Z","nextAttemptAt":"2026-11-24T09:11:00Z","failedAttempts":1,"lastAttemptAt":null}""",
        "Published", """, "relatedPendingWakes":[]""")]
    [InlineData("null", "No retained delivery metadata is available.", "")]
    public async Task Displays_original_wake_delivery_independently_of_placement_status(string wake, string expected, string pendingWakesField)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":2,"revision":10,
             "acceptedAt":null,"terminalAt":null,"originalWake":{{wake}}{{pendingWakesField}}
            }
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        var placement = page.Find("dl").TextContent;
        var originalWake = page.Find("section[aria-labelledby='gallery-original-wake-heading']").TextContent;
        var inboxLinks = page.FindAll("section[aria-labelledby='gallery-original-wake-heading'] a")
            .Select(link => link.GetAttribute("href")).ToArray();

        // Assert
        placement.ShouldContain("Rejected after publication", StringComparison.Ordinal);
        placement.ShouldNotContain("Committed event ID", StringComparison.Ordinal);
        originalWake.ShouldContain(expected, StringComparison.Ordinal);
        inboxLinks.ShouldBe([$"operations/messaging/catalog-delivery?view=inbox&eventId={intentId:D}#catalog-inbox-heading"]);
        page.FindAll("#gallery-pending-wakes-heading").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("null", 0, 0, "Pending", "Pending", false)]
    [InlineData("\"2026-11-24T09:11:29Z\"", 0, 2, "Retry due", "Pending", true)]
    [InlineData("\"2026-11-24T10:11:30+01:00\"", 0, 2, "Retry due", "Pending", true)]
    [InlineData("\"2026-11-24T09:11:30Z\"", 2, 2, "Retry due", "Rejected after publication", false)]
    [InlineData("\"2026-11-24T09:11:30Z\"", 0, 1, "Retry due", "Pending", false)]
    [InlineData("\"2026-11-24T09:11:31Z\"", 0, 2, "Retry scheduled", "Pending", false)]
    public async Task Reports_the_delivery_state_of_the_associated_pending_wake(
        string nextAttempt, int placementStatus, int bootstrapFailures, string expectedDelivery, string expectedPlacement, bool expectedCandidate)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var bootstrapId = Guid.CreateVersion7();
        var bootstrapLastAttempt = nextAttempt == "null" ? "null" : "\"2026-11-24T09:11:10Z\"";
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":{{placementStatus}},
             "revision":null,"acceptedAt":"2026-11-24T09:10:00Z","terminalAt":null,
             "originalWake":{"recordedAt":"2026-11-24T09:10:00Z","publishedAt":null,"failedAttempts":1,
                 "lastAttemptAt":"2026-11-24T09:10:30Z","nextAttemptAt":"2026-11-24T09:12:00Z"},
             "relatedPendingWakes":[
             {"eventId":"{{intentId:D}}","recordedAt":"2026-11-24T09:10:00Z","failedAttempts":1,
                 "lastAttemptAt":"2026-11-24T09:10:30Z","nextAttemptAt":"2026-11-24T09:12:00Z"},
             {"eventId":"{{bootstrapId:D}}","recordedAt":"2026-11-24T09:11:00Z","failedAttempts":{{bootstrapFailures}},
                 "lastAttemptAt":{{bootstrapLastAttempt}},"nextAttemptAt":{{nextAttempt}}}
            ]}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        const string wakeTable = "section[aria-labelledby='gallery-pending-wakes-heading'] table";
        var rows = page.FindAll($"{wakeTable} tbody tr");
        var bootstrap = rows.ShouldHaveSingleItem(row => row.QuerySelector("code")?.TextContent == bootstrapId.ToString("D"));
        var original = rows.ShouldHaveSingleItem(row => row.QuerySelector("code")?.TextContent == intentId.ToString("D"));
        var placementState = page.FindAll("dl dt").ShouldHaveSingleItem(term => term.TextContent == "State");
        var displayedCandidate = page.Markup.Contains(DueWakeCandidateText, StringComparison.Ordinal);

        // Assert
        placementState.NextElementSibling.ShouldNotBeNull().TextContent.ShouldBe(expectedPlacement);
        page.Find("section[aria-labelledby='gallery-original-wake-heading']").TextContent.ShouldContain("Retry scheduled", StringComparison.Ordinal);
        bootstrap.TextContent.ShouldContain(expectedDelivery, StringComparison.Ordinal);
        original.TextContent.ShouldContain("Retry scheduled", StringComparison.Ordinal);
        page.FindAll($"{wakeTable} thead th").Select(header => header.TextContent).ShouldContain("Delivery state");
        displayedCandidate.ShouldBe(expectedCandidate);
    }

    [Theory]
    [InlineData("null", "null", "Unavailable", null)]
    [InlineData("\"2026-11-24T09:10:00Z\"", "null", "0h 1m", "2026-11-24T09:10:00.0000000+00:00")]
    [InlineData("\"2026-11-23T07:00:00Z\"", "\"2026-11-23T08:10:00Z\"", "1d 1h 1m", "2026-11-23T08:10:00.0000000+00:00")]
    [InlineData("\"2026-11-24T09:10:00Z\"", "\"2026-11-24T09:12:00Z\"", "Unavailable", null)]
    public async Task Displays_updated_age_for_the_latest_recorded_placement_transition(
        string acceptedAt, string terminalAt, string expectedAge, string? expectedTimestamp)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}","status":2,
             "revision":null,"acceptedAt":{{acceptedAt}},"terminalAt":{{terminalAt}}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        var label = page.FindAll("dl:first-of-type > dt").Where(element => element.TextContent == "Updated age").ShouldHaveSingleItem();
        var value = label.NextElementSibling;

        // Assert
        value.ShouldNotBeNull();
        var timestamp = value.QuerySelector("time")?.GetAttribute("datetime");
        value.TextContent.Trim().ShouldBe(expectedAge);
        timestamp.ShouldBe(expectedTimestamp);
    }

    [Fact]
    public async Task Invalid_tour_or_sequence_shows_validation_without_a_request()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();

        // Act
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = "invalid-tour" });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "0" });
        await page.Find("form").SubmitAsync();

        // Assert
        page.Find("[role='alert']").TextContent.ShouldBe("Enter a Catalog tour ID and a positive placement sequence.");
        transport.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, 7, 0, false)]
    [InlineData(false, 8, 0, false)]
    [InlineData(false, 7, 99, false)]
    [InlineData(false, 7, 0, true)]
    [InlineData(false, 7, null, false)]
    public async Task Does_not_render_a_foreign_or_malformed_successful_placement(
        bool wrongTour, long responseSequence, int? status, bool emptyIntent)
    {
        // Arrange
        var requestedTour = Guid.CreateVersion7();
        var responseTour = wrongTour ? Guid.CreateVersion7() : requestedTour;
        var intentId = emptyIntent ? Guid.Empty : Guid.CreateVersion7();
        var statusField = status.HasValue ? $"\"status\":{status.Value}," : string.Empty;
        var body = $$"""
            {"catalogTourId":"{{responseTour:D}}","sequence":{{responseSequence}},"intentId":"{{intentId:D}}",
             {{statusField}}"revision":null,"acceptedAt":null,"terminalAt":null,
             "relatedPendingWakes":[{"eventId":"{{Guid.CreateVersion7():D}}","recordedAt":"2026-11-24T09:10:00Z",
                 "failedAttempts":2,"lastAttemptAt":"2026-11-24T09:11:00Z","nextAttemptAt":"2026-11-24T09:11:30Z"}]}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = requestedTour.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();

        // Assert
        page.Find("[role='alert']").TextContent.ShouldBe("Gallery placement status could not be loaded.");
        page.FindAll("dl").ShouldBeEmpty();
        page.FindAll("a[download], a[href^='operations/messaging/catalog-delivery?view=inbox']").ShouldBeEmpty();
        page.Markup.ShouldNotContain(DueWakeCandidateText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Displays_a_verified_pending_placement_with_unknown_historical_timestamps()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var intentId = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{intentId:D}}",
             "status":0,"revision":null,"acceptedAt":null,"terminalAt":null}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();

        // Assert
        var placement = page.Find("dl").TextContent;
        placement.ShouldContain("Pending", StringComparison.Ordinal);
        placement.ShouldContain("Unavailable", StringComparison.Ordinal);
        placement.ShouldContain(intentId.ToString("D"), StringComparison.Ordinal);
        page.Find("[download]").GetAttribute("href").ShouldBe($"/operations/gallery/{tourId:D}/placements/7/diagnostics");
        page.Markup.ShouldNotContain(DueWakeCandidateText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Token_exchange_failure_allows_a_fresh_lookup_without_exposing_exception_details()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var attempt = 0;
        using var transport = new RecordingHttpMessageHandler(_ => ++attempt == 1
            ? throw new UserTokenUnavailableException("synthetic-session-detail")
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-status-tour").ChangeAsync(new ChangeEventArgs { Value = tourId.ToString("D") });
        await page.Find("#gallery-status-sequence").ChangeAsync(new ChangeEventArgs { Value = "7" });

        // Act
        await page.Find("form").SubmitAsync();
        var error = page.Find("[role='alert']").TextContent;
        var failureMarkup = page.Markup;
        var disabled = page.Find("button[type='submit']").HasAttribute("disabled");
        await page.Find("form").SubmitAsync();

        // Assert
        error.ShouldBe("Gallery placement status could not be loaded.");
        failureMarkup.ShouldNotContain("synthetic-session-detail", StringComparison.Ordinal);
        disabled.ShouldBeFalse();
        page.Find("[role='status']").TextContent.ShouldContain("No Gallery placement", StringComparison.Ordinal);
        transport.Requests.Count.ShouldBe(2);
    }
}
