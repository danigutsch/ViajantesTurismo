using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class CatalogDeliveryTests : BunitContext
{
    private const string FocusInterop = "Blazor._internal.domWrapper.focus";
    private static readonly DateTimeOffset RetryAt = new(2026, 11, 20, 14, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(RetryAt.AddMinutes(-1));

    public CatalogDeliveryTests()
    {
        Services.AddSingleton<TimeProvider>(_clock);
        JSInterop.SetupVoid(FocusInterop, _ => true).SetVoidResult();
    }

    private const string EmptyPage = """
        {"rows":[],"nextRecordedAt":null,"nextStorageId":null}
        """;

    private const string FirstPage = """
        {
          "rows":[
            {
              "storageId":"22222222-2222-2222-2222-222222222222",
              "eventId":"event-retry","eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T12:00:00Z",
              "attempts":2,"lastAttemptAt":"2026-11-20T13:00:00Z",
              "nextAttemptAt":"2026-11-20T14:00:00Z","terminalAt":null,
              "payload":"synthetic-body-marker","lastError":"synthetic-error-marker"
            },
            {
              "storageId":"11111111-1111-1111-1111-111111111111",
              "eventId":"event-pending","eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T12:00:00Z",
              "attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null
            }
          ],
          "nextRecordedAt":"2026-11-20T12:00:00Z",
          "nextStorageId":"11111111-1111-1111-1111-111111111111"
        }
        """;

    private const string LastPage = """
        {
          "rows":[
            {
              "storageId":"33333333-3333-3333-3333-333333333333",
              "eventId":"event-terminal","eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T11:00:00Z",
              "attempts":3,"lastAttemptAt":"2026-11-20T12:30:00Z",
              "nextAttemptAt":null,"terminalAt":"2026-11-20T12:30:00Z"
            }
          ],
          "nextRecordedAt":null,
          "nextStorageId":null
        }
        """;

    [Fact]
    public void Catalog_delivery_page_requires_the_admin_role()
    {
        // Arrange
        var pageType = typeof(CatalogDelivery);

        // Act
        var route = pageType.GetCustomAttributes(typeof(RouteAttribute), false).OfType<RouteAttribute>().ShouldHaveSingleItem();
        var authorization = pageType.GetCustomAttributes(typeof(AuthorizeAttribute), false).OfType<AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/operations/messaging/catalog-delivery");
        authorization.Roles.ShouldBe("Admin");
    }

    [Fact]
    public async Task Shows_only_metadata_and_keeps_submitted_filters_when_browsing_continuations()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(request =>
        {
            var query = QueryHelpers.ParseQuery(request.Message.RequestUri?.Query);
            var response = query.ContainsKey("eventId") ? EmptyPage
                : query.ContainsKey("afterStorageId") ? LastPage : FirstPage;
            return HttpResponseFactory.Json(response, HttpStatusCode.OK);
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var page = Render<CatalogDelivery>();
        var heading = (AngleSharp.Dom.IElement)page.Find("h1").Clone();
        var initialFocus = JSInterop.Invocations[FocusInterop].Count;
        var initialRows = page.FindAll("tbody tr").Select(row => row.TextContent).ToArray();
        var initialMarkup = page.Markup;
        await page.Find("#delivery-event-type").ChangeAsync(new ChangeEventArgs { Value = " Tours.Changed+V1 " });
        await page.Find("#delivery-recorded-since").ChangeAsync(new ChangeEventArgs { Value = "2026-11-20" });
        await page.Find("form").SubmitAsync();
        await page.Find("#delivery-event-id").ChangeAsync(new ChangeEventArgs { Value = " event & detail+ " });
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").ClickAsync();
        var lastRow = page.Find("tbody tr").TextContent;
        var lastNextDisabled = page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").HasAttribute("disabled");
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "First page").ClickAsync();
        var firstPageRowCount = page.FindAll("tbody tr").Count;
        await page.Find("form").SubmitAsync();

        // Assert
        initialFocus.ShouldBe(0);
        var pagingFocus = JSInterop.Invocations[FocusInterop];
        pagingFocus.ShouldHaveCount(2);
        pagingFocus[0].Arguments[0].ShouldBeElementReferenceTo(heading);
        pagingFocus[1].Arguments[0].ShouldBeElementReferenceTo(heading);
        initialRows.ShouldHaveCount(2);
        initialRows[0].ShouldContain("event-retry", StringComparison.Ordinal);
        initialRows[0].ShouldContain("Retry scheduled", StringComparison.Ordinal);
        initialRows[0].ShouldContain("Consumption failed", StringComparison.Ordinal);
        initialRows[0].ShouldContain("2026-11-20 14:00:00Z", StringComparison.Ordinal);
        initialRows[1].ShouldContain("Pending", StringComparison.Ordinal);
        initialRows[1].ShouldNotContain("Consumption failed", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("synthetic-body-marker", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("synthetic-error-marker", StringComparison.Ordinal);
        lastRow.ShouldContain("Terminal failure", StringComparison.Ordinal);
        lastRow.ShouldContain("Final consumption failure", StringComparison.Ordinal);
        lastRow.ShouldContain("2026-11-20 12:30:00Z", StringComparison.Ordinal);
        lastNextDisabled.ShouldBeTrue();
        firstPageRowCount.ShouldBe(2);
        page.Find("[role='status']").TextContent.ShouldBe("No unfinished Catalog deliveries match this search.");

        var requests = transport.Requests.Select(request => request.Message).ToArray();
        requests.ShouldHaveCount(5);
        foreach (var request in requests)
        {
            request.Method.ShouldBe(HttpMethod.Get);
            request.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/messaging/catalog-delivery");
            request.Content.ShouldBeNull();
        }

        QueryHelpers.ParseQuery(requests[0].RequestUri?.Query).Keys.ToArray().ShouldBe(["limit"]);
        var filtered = QueryHelpers.ParseQuery(requests[1].RequestUri?.Query);
        filtered["limit"].ToString().ShouldBe("25");
        filtered["eventType"].ToString().ShouldBe("Tours.Changed+V1");
        filtered["recordedSince"].ToString().ShouldBe("2026-11-20T00:00:00.0000000+00:00");
        var continuation = QueryHelpers.ParseQuery(requests[2].RequestUri?.Query);
        continuation.ContainsKey("eventId").ShouldBeFalse();
        continuation["eventType"].ToString().ShouldBe(filtered["eventType"].ToString());
        continuation["recordedSince"].ToString().ShouldBe(filtered["recordedSince"].ToString());
        continuation["afterRecordedAt"].ToString().ShouldBe("2026-11-20T12:00:00.0000000+00:00");
        continuation["afterStorageId"].ToString().ShouldBe("11111111-1111-1111-1111-111111111111");
        requests[3].RequestUri.ShouldBe(requests[1].RequestUri);
        var eventSearch = QueryHelpers.ParseQuery(requests[4].RequestUri?.Query);
        eventSearch["eventId"].ToString().ShouldBe("event & detail+");
        eventSearch.ContainsKey("afterRecordedAt").ShouldBeFalse();
        eventSearch.ContainsKey("afterStorageId").ShouldBeFalse();
    }

    [Fact]
    public async Task Existing_delivery_page_can_inspect_completed_duplicates_and_page_by_the_catalog_cursor()
    {
        // Arrange
        const string inbox = "/api/v1/catalog/operations/messaging/inbox";
        const string newest = "33333333-3333-3333-3333-333333333333";
        const string older = "22222222-2222-2222-2222-222222222222";
        using var adminTransport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyPage, HttpStatusCode.OK));
        using var catalogTransport = new RecordingHttpMessageHandler(request =>
        {
            var uri = request.Message.RequestUri.ShouldNotBeNull();
            if (uri.AbsolutePath == inbox)
            {
                return HttpResponseFactory.Json(
                    """{"entriesWithDuplicates":2,"duplicateCount":3,"lastObservedAt":"2026-11-24T09:20:00Z"}""", HttpStatusCode.OK);
            }

            var query = QueryHelpers.ParseQuery(uri.Query);
            var page = query.ContainsKey("afterEventId") || query.ContainsKey("eventId")
                ? $$"""{"rows":[{"eventId":"{{older}}","duplicateCount":1,"lastObservedAt":"2026-11-24T09:10:00Z"}],"nextObservedAt":null,"nextEventId":null}"""
                : $$"""{"rows":[{"eventId":"{{newest}}","duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z","payload":"synthetic-secret"}],"nextObservedAt":"2026-11-24T09:20:00Z","nextEventId":"{{newest}}"}""";
            return HttpResponseFactory.Json(page, HttpStatusCode.OK);
        });
        using var adminClient = new HttpClient(adminTransport) { BaseAddress = new Uri("https://admin.example.test") };
        using var catalogClient = new HttpClient(catalogTransport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(adminClient));
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(catalogClient));
        // Act
        var page = Render<CatalogDelivery>();
        var initialRequests = catalogTransport.Requests.Count;
        var initialFocus = JSInterop.Invocations[FocusInterop].Count;
        var inboxHeading = (AngleSharp.Dom.IElement)page.Find("#catalog-inbox-heading").Clone();
        await page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Inspect completed duplicates").ClickAsync();
        var newestMarkup = page.Markup;
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next duplicate page").ClickAsync();
        var olderRow = page.Find("tbody tr").TextContent;
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "First duplicate page").ClickAsync();
        await page.Find("#catalog-inbox-event-id").ChangeAsync(new ChangeEventArgs { Value = "bad-id" });
        await page.FindAll("form")[1].SubmitAsync();
        var validation = page.Find("[role='alert']").TextContent;
        await page.Find("#catalog-inbox-event-id").ChangeAsync(new ChangeEventArgs { Value = older });
        await page.FindAll("form")[1].SubmitAsync();

        // Assert
        initialRequests.ShouldBe(0);
        initialFocus.ShouldBe(0);
        var inboxFocus = JSInterop.Invocations[FocusInterop];
        inboxFocus.ShouldHaveCount(3);
        inboxFocus[0].Arguments[0].ShouldBeElementReferenceTo(inboxHeading);
        inboxFocus[1].Arguments[0].ShouldBeElementReferenceTo(inboxHeading);
        inboxFocus[2].Arguments[0].ShouldBeElementReferenceTo(inboxHeading);
        page.Find("#catalog-inbox-heading").GetAttribute("tabindex").ShouldBe("-1");
        newestMarkup.ShouldContain("3 duplicate deliveries across 2 retained events.", StringComparison.Ordinal);
        newestMarkup.ShouldNotContain("Inbox consumer", StringComparison.Ordinal);
        newestMarkup.ShouldContain(newest, StringComparison.Ordinal);
        newestMarkup.ShouldNotContain("synthetic-secret", StringComparison.Ordinal);
        olderRow.ShouldContain(older, StringComparison.Ordinal);
        validation.ShouldBe("Enter a valid event ID.");
        page.Find("tbody tr").TextContent.ShouldContain(older, StringComparison.Ordinal);
        adminTransport.Requests.ShouldHaveSingleItem();
        var requests = catalogTransport.Requests.Select(item => item.Message.RequestUri.ShouldNotBeNull()).ToArray();
        requests.ShouldHaveCount(5);
        requests[0].AbsolutePath.ShouldBe(inbox);
        requests[1].AbsolutePath.ShouldBe($"{inbox}/duplicates");
        QueryHelpers.ParseQuery(requests[1].Query)["limit"].ToString().ShouldBe("25");
        QueryHelpers.ParseQuery(requests[2].Query)["afterEventId"].ToString().ShouldBe(newest);
        QueryHelpers.ParseQuery(requests[2].Query)["afterObservedAt"].ToString().ShouldBe("2026-11-24T09:20:00.0000000+00:00");
        requests[3].ShouldBe(requests[1]);
        QueryHelpers.ParseQuery(requests[4].Query)["eventId"].ToString().ShouldBe(older);
    }

    [Theory]
    [InlineData("22222222-2222-2222-2222-222222222222", true)]
    [InlineData("bad-id", false)]
    [InlineData("", false)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    public void A_linked_inbox_reads_only_the_exact_valid_event(string linkedId, bool valid)
    {
        // Arrange
        const string inbox = "/api/v1/catalog/operations/messaging/inbox";
        const string inboxPage = """{"rows":[],"nextObservedAt":null,"nextEventId":null}""";
        const string summary = """{"consumerName":"catalog","entriesWithDuplicates":0,"duplicateCount":0,"lastObservedAt":null}""";
        using var adminTransport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyPage, HttpStatusCode.OK));
        using var catalogTransport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == inbox ? summary : inboxPage, HttpStatusCode.OK));
        using var adminClient = new HttpClient(adminTransport) { BaseAddress = new Uri("https://admin.example.test") };
        using var catalogClient = new HttpClient(catalogTransport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(adminClient));
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(catalogClient));
        var navigation = this.GetNavigationManager();
        navigation.NavigateTo($"operations/messaging/catalog-delivery?view=inbox&eventId={linkedId}#catalog-inbox-heading");

        // Act
        var page = Render<CatalogDelivery>();
        var inboxRequests = catalogTransport.Requests.Select(request => request.Message.RequestUri.ShouldNotBeNull())
            .Where(uri => uri.AbsolutePath == $"{inbox}/duplicates").ToArray();
        var shownEventId = page.Find("#catalog-inbox-event-id").GetAttribute("value");

        // Assert
        if (valid)
        {
            var query = QueryHelpers.ParseQuery(inboxRequests.ShouldHaveSingleItem().Query);
            query["eventId"].ToString().ShouldBe(linkedId);
            shownEventId.ShouldBe(linkedId);
            page.Find("section[aria-labelledby='catalog-inbox-heading'] [role='status']")
                .TextContent.ShouldBe("No completed duplicates match this search.");

            // Act
            const string otherWake = "33333333-3333-3333-3333-333333333333";
            navigation.NavigateTo($"operations/messaging/catalog-delivery?view=inbox&eventId={otherWake}#catalog-inbox-heading");

            // Assert
            page.WaitForAssertion(() => page.Find("#catalog-inbox-event-id").GetAttribute("value").ShouldBe(otherWake));
            var queries = catalogTransport.Requests.Select(request => request.Message.RequestUri.ShouldNotBeNull())
                .Where(uri => uri.AbsolutePath == $"{inbox}/duplicates")
                .Select(uri => QueryHelpers.ParseQuery(uri.Query)["eventId"].ToString()).ToArray();
            queries.ShouldBe([linkedId, otherWake]);
        }
        else
        {
            inboxRequests.ShouldBeEmpty();
            page.Find("[role='alert']").TextContent.ShouldBe("Enter a valid event ID.");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_inbox_identifies_its_consumer_when_empty_or_populated(bool hasDuplicates)
    {
        // Arrange
        var summary = hasDuplicates
            ? """{"consumerName":"catalog","entriesWithDuplicates":1,"duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}"""
            : """{"consumerName":"catalog","entriesWithDuplicates":0,"duplicateCount":0,"lastObservedAt":null}""";
        var page = hasDuplicates
            ? """{"rows":[{"eventId":"33333333-3333-3333-3333-333333333333","duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}],"nextObservedAt":null,"nextEventId":null}"""
            : """{"rows":[],"nextObservedAt":null,"nextEventId":null}""";
        using var transport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath.EndsWith("/inbox", StringComparison.Ordinal) == true ? summary : page,
            HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(client));

        // Act
        var component = Render<CatalogInboxEvidence>();

        // Assert
        var paragraphs = component.FindAll("p").Select(paragraph => paragraph.TextContent).ToArray();
        paragraphs.ShouldContain("Inbox consumer catalog");
        paragraphs.ShouldContain(hasDuplicates
            ? "2 duplicate deliveries across 1 retained events."
            : "No completed duplicates match this search.");
    }

    [Fact]
    public async Task A_duplicate_age_search_keeps_its_UTC_cutoff_when_paging_and_restarts_changed_searches()
    {
        // Arrange
        const string eventId = "33333333-3333-3333-3333-333333333333";
        using var transport = new RecordingHttpMessageHandler(request =>
        {
            var uri = request.Message.RequestUri.ShouldNotBeNull();
            if (uri.AbsolutePath.EndsWith("/inbox", StringComparison.Ordinal))
            {
                return HttpResponseFactory.Json(
                    """{"entriesWithDuplicates":1,"duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}""", HttpStatusCode.OK);
            }

            var query = QueryHelpers.ParseQuery(uri.Query);
            var page = query.ContainsKey("afterEventId")
                ? """{"rows":[],"nextObservedAt":null,"nextEventId":null}"""
                : $$"""{"rows":[{"eventId":"{{eventId}}","duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}],"nextObservedAt":"2026-11-24T09:20:00Z","nextEventId":"{{eventId}}"}""";
            return HttpResponseFactory.Json(page, HttpStatusCode.OK);
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(client));
        var component = Render<CatalogInboxEvidence>();

        // Act
        await component.Find("#catalog-inbox-observed-since").ChangeAsync(new ChangeEventArgs { Value = "2026-11-24T09:00" });
        await component.Find("form").SubmitAsync();
        await component.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next duplicate page").ClickAsync();
        await component.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "First duplicate page").ClickAsync();
        await component.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next duplicate page").ClickAsync();
        await component.Find("#catalog-inbox-observed-since").ChangeAsync(new ChangeEventArgs { Value = "2026-11-24T09:15" });
        await component.Find("form").SubmitAsync();
        await component.Find("#catalog-inbox-observed-since").ChangeAsync(new ChangeEventArgs { Value = "" });
        await component.Find("form").SubmitAsync();

        // Assert
        component.Find("label[for='catalog-inbox-observed-since']").TextContent.ShouldBe("Observed since (UTC)");
        var requests = transport.Requests.Select(item => item.Message.RequestUri.ShouldNotBeNull()).ToArray();
        requests.ShouldHaveCount(8);
        QueryHelpers.ParseQuery(requests[1].Query).ContainsKey("observedSince").ShouldBeFalse();
        var filtered = QueryHelpers.ParseQuery(requests[2].Query);
        var continuation = QueryHelpers.ParseQuery(requests[3].Query);
        filtered["observedSince"].ToString().ShouldBe("2026-11-24T09:00:00.0000000+00:00");
        continuation["observedSince"].ShouldBe(filtered["observedSince"]);
        continuation["afterEventId"].ToString().ShouldBe(eventId);
        requests[4].ShouldBe(requests[2]);
        requests[5].ShouldBe(requests[3]);
        var updated = QueryHelpers.ParseQuery(requests[6].Query);
        updated["observedSince"].ToString().ShouldBe("2026-11-24T09:15:00.0000000+00:00");
        updated.ContainsKey("afterObservedAt").ShouldBeFalse();
        updated.ContainsKey("afterEventId").ShouldBeFalse();
        requests[7].ShouldBe(requests[1]);
    }

    [Theory]
    [InlineData(false, "No completed duplicates match this search.")]
    [InlineData(true, "Catalog inbox history is unavailable.")]
    public async Task Invalid_event_ID_hides_the_previous_pager_until_the_search_is_corrected(bool continuationUnavailable, string continuationMessage)
    {
        // Arrange
        const string eventId = "33333333-3333-3333-3333-333333333333";
        const string pager = "nav[aria-label='Catalog inbox pages']";
        const string rows = $$"""[{"eventId":"{{eventId}}","duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}]""";
        var continuation = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new List<Uri>();
        using var transport = new DelegateHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri.ShouldNotBeNull();
            requests.Add(uri);
            if (uri.AbsolutePath.EndsWith("/inbox", StringComparison.Ordinal))
            {
                return Task.FromResult(HttpResponseFactory.Json(
                    """{"entriesWithDuplicates":1,"duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}""", HttpStatusCode.OK));
            }

            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.ContainsKey("afterEventId"))
            {
                return continuation.Task;
            }

            var page = query.ContainsKey("eventId")
                ? $$"""{"rows":{{rows}},"nextObservedAt":null,"nextEventId":null}"""
                : $$"""{"rows":{{rows}},"nextObservedAt":"2026-11-24T09:20:00Z","nextEventId":"{{eventId}}"}""";
            return Task.FromResult(HttpResponseFactory.Json(page, HttpStatusCode.OK));
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(client));
        var component = Render<CatalogInboxEvidence>();

        // Act
        var pendingPage = component.FindAll($"{pager} button")
            .ShouldHaveSingleItem(button => button.TextContent == "Next duplicate page").ClickAsync();
        component.WaitForState(() => component.FindAll("[role='status']").Any(status => status.TextContent == "Loading Catalog duplicates…"));
        var loadingPagerCount = component.FindAll(pager).Count;
        continuation.SetResult(continuationUnavailable
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : HttpResponseFactory.Json("""{"rows":[],"nextObservedAt":null,"nextEventId":null}""", HttpStatusCode.OK));
        await pendingPage;
        var firstPageWasDisabled = component.FindAll($"{pager} button")
            .ShouldHaveSingleItem(button => button.TextContent == "First duplicate page").HasAttribute("disabled");
        var message = component.Find(continuationUnavailable ? "[role='alert']" : "[role='status']").TextContent;
        var requestCount = requests.Count;
        await component.Find("#catalog-inbox-event-id").ChangeAsync(new ChangeEventArgs { Value = "bad-id" });
        await component.Find("form").SubmitAsync();
        var validation = component.Find("[role='alert']").TextContent;
        var invalidPagerCount = component.FindAll(pager).Count;
        var invalidRequestCount = requests.Count;
        await component.Find("#catalog-inbox-event-id").ChangeAsync(new ChangeEventArgs { Value = eventId });
        await component.Find("form").SubmitAsync();

        // Assert
        loadingPagerCount.ShouldBe(0);
        message.ShouldBe(continuationMessage);
        firstPageWasDisabled.ShouldBeFalse();
        validation.ShouldBe("Enter a valid event ID.");
        invalidPagerCount.ShouldBe(0);
        invalidRequestCount.ShouldBe(requestCount);
        requests.Count.ShouldBe(requestCount + 1);
        var correctedQuery = QueryHelpers.ParseQuery(requests[^1].Query);
        correctedQuery["eventId"].ToString().ShouldBe(eventId);
        correctedQuery.ContainsKey("afterEventId").ShouldBeFalse();
        correctedQuery.ContainsKey("afterObservedAt").ShouldBeFalse();
        component.Find("tbody tr").TextContent.ShouldContain(eventId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unavailable_catalog_tokens_leave_the_inbox_evidence_in_a_safe_error_state()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(_ => throw new UserTokenUnavailableException("synthetic-token-error"));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(client));

        // Act
        var component = Render<CatalogInboxEvidence>();
        await component.Find("#catalog-inbox-observed-since").ChangeAsync(new ChangeEventArgs { Value = "2026-11-24T09:00" });
        await component.Find("form").SubmitAsync();

        // Assert
        component.FindAll("[role='alert']").Select(alert => alert.TextContent)
            .ShouldBe(["Catalog inbox totals are unavailable.", "Catalog inbox history is unavailable."]);
        component.Markup.ShouldNotContain("synthetic-token-error", StringComparison.Ordinal);
        component.FindAll("[role='status']").ShouldBeEmpty();
        transport.Requests.ShouldHaveCount(3);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    public void Failure_summary_uses_retained_attempt_facts_and_never_echoes_type_like_error_text(int attempts, bool recordedTime)
    {
        // Arrange
        const string legacyError = "System.IO.IOException";
        var response = FirstPage.Replace("\"attempts\":2", FormattableString.Invariant($"\"attempts\":{attempts}"), StringComparison.Ordinal)
            .Replace("synthetic-error-marker", legacyError, StringComparison.Ordinal);
        if (!recordedTime)
        {
            response = response.Replace("\"lastAttemptAt\":\"2026-11-20T13:00:00Z\"", "\"lastAttemptAt\":null", StringComparison.Ordinal);
        }
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(response, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var page = Render<CatalogDelivery>();

        // Assert
        var row = page.Find("tbody tr:first-child").TextContent;
        row.ShouldContain("Consumption failed", StringComparison.Ordinal);
        row.ShouldNotContain(legacyError, StringComparison.Ordinal);
        row.ShouldNotContain("synthetic-body-marker", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task A_retry_becomes_due_at_its_scheduled_time_on_the_next_snapshot(int elapsedMinutes)
    {
        // Arrange
        const string retryRow = "tbody tr:first-child";
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(FirstPage, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var page = Render<CatalogDelivery>();
        var initialSnapshot = page.Find(retryRow).TextContent;

        // Act
        _clock.CurrentInstant = RetryAt.AddMinutes(elapsedMinutes);
        await page.Find("#delivery-event-type").ChangeAsync(new ChangeEventArgs { Value = "Tours.Changed+V1" });
        var beforeRefresh = page.Find(retryRow).TextContent;
        await page.Find("form").SubmitAsync();

        // Assert
        initialSnapshot.ShouldContain("Retry scheduled", StringComparison.Ordinal);
        beforeRefresh.ShouldContain("Retry scheduled", StringComparison.Ordinal);
        var refreshed = page.Find(retryRow).TextContent;
        refreshed.ShouldContain("Retry due", StringComparison.Ordinal);
        refreshed.ShouldContain("2026-11-20 14:00:00Z", StringComparison.Ordinal);
        transport.Requests.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_read_shows_a_safe_status_and_a_search_can_recover_to_an_empty_page(bool tokenUnavailable)
    {
        // Arrange
        var status = HttpStatusCode.ServiceUnavailable;
        using var transport = new RecordingHttpMessageHandler(_ =>
        {
            if (status == HttpStatusCode.OK)
            {
                return HttpResponseFactory.Json(EmptyPage, status);
            }

            if (tokenUnavailable)
            {
                throw new UserTokenUnavailableException("synthetic-private-provider-detail");
            }

            return HttpResponseFactory.Json("""{"detail":"synthetic-private-provider-detail"}""", status);
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var page = Render<CatalogDelivery>();
        var failureMessage = page.Find("[role='alert']").TextContent;
        var failureMarkup = page.Markup;
        var searchDisabled = page.Find("button[type='submit']").HasAttribute("disabled");
        status = HttpStatusCode.OK;

        // Act
        await page.Find("form").SubmitAsync();

        // Assert
        failureMessage.ShouldBe("Catalog delivery metadata is unavailable.");
        failureMarkup.ShouldNotContain("synthetic-private-provider-detail", StringComparison.Ordinal);
        searchDisabled.ShouldBeFalse();
        page.Find("[role='status']").TextContent.ShouldBe("No unfinished Catalog deliveries match this search.");
        page.FindAll("table").ShouldBeEmpty();
        page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").HasAttribute("disabled").ShouldBeTrue();
    }
}
