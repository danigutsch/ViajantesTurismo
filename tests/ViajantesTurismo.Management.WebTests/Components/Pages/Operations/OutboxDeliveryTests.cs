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
public sealed class OutboxDeliveryTests : BunitContext
{
    private const string FocusInterop = "Blazor._internal.domWrapper.focus";
    private static readonly DateTimeOffset RetryAt = new(2026, 11, 20, 14, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(RetryAt.AddMinutes(-1));

    public OutboxDeliveryTests()
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
              "nextAttemptAt":"2026-11-20T14:00:00Z",
              "payload":"synthetic-body-marker","lastError":"synthetic-error-marker"
            },
            {
              "storageId":"11111111-1111-1111-1111-111111111111",
              "eventId":"event-pending","eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T12:00:00Z",
              "attempts":0,"lastAttemptAt":null,"nextAttemptAt":null
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
              "eventId":"event-pending-second","eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T11:00:00Z",
              "attempts":3,"lastAttemptAt":"2026-11-20T12:30:00Z",
              "nextAttemptAt":null
            }
          ],
          "nextRecordedAt":null,
          "nextStorageId":null
        }
        """;

    [Fact]
    public void Outbox_delivery_page_requires_the_admin_role()
    {
        // Arrange
        var pageType = typeof(OutboxDelivery);

        // Act
        var route = pageType.GetCustomAttributes(typeof(RouteAttribute), false).OfType<RouteAttribute>().ShouldHaveSingleItem();
        var authorization = pageType.GetCustomAttributes(typeof(AuthorizeAttribute), false).OfType<AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/operations/messaging/outbox");
        authorization.Roles.ShouldBe("Admin");
    }

    [Theory]
    [InlineData("22222222-2222-2222-2222-222222222222", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("", false)]
    public void A_retained_outbox_link_requests_only_a_valid_exact_event_ID(string eventId, bool valid)
    {
        // Arrange
        const string retainedEmpty = """{"rows":[],"nextRecordedAt":null,"nextStorageId":null,"includesPublished":true}""";
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(retainedEmpty, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var navigation = this.GetNavigationManager();
        navigation.NavigateTo($"operations/messaging/outbox?view=retained&eventId={eventId}");

        // Act
        var page = Render<OutboxDelivery>();

        // Assert
        if (valid)
        {
            var request = transport.Requests.ShouldHaveSingleItem().Message;
            var query = QueryHelpers.ParseQuery(request.RequestUri?.Query);
            query["eventId"].ToString().ShouldBe(eventId);
            query["includePublished"].ToString().ShouldBe("true");
            page.Find("#outbox-event-id").GetAttribute("value").ShouldBe(eventId);
            page.Find("[role='status']").TextContent.ShouldBe("No retained outbox message matches this search.");

            // Act
            const string nextEventId = "33333333-3333-3333-3333-333333333333";
            navigation.NavigateTo($"operations/messaging/outbox?view=retained&eventId={nextEventId}");

            // Assert
            page.WaitForAssertion(() => page.Find("#outbox-event-id").GetAttribute("value").ShouldBe(nextEventId));
            var ids = transport.Requests.Select(item => QueryHelpers.ParseQuery(item.Message.RequestUri?.Query)["eventId"].ToString());
            ids.ShouldBe([eventId, nextEventId]);
        }
        else
        {
            transport.Requests.ShouldBeEmpty();
            page.Find("[role='alert']").TextContent.ShouldBe("Enter an exact event ID to include retained published messages.");
        }
    }

    [Fact]
    public async Task Retained_publication_search_requires_an_exact_event_ID_and_shows_only_safe_evidence()
    {
        // Arrange
        const string eventId = "event-retained+1";
        const string published = """
            {"rows":[{"storageId":"22222222-2222-2222-2222-222222222222","eventId":"event-retained+1",
              "eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T12:00:00Z","publishedAt":"2026-11-20T12:15:00Z",
              "attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"payload":"synthetic-body-marker"}],
              "nextRecordedAt":null,"nextStorageId":null,"includesPublished":true}
            """;
        const string retainedEmpty = """{"rows":[],"nextRecordedAt":null,"nextStorageId":null,"includesPublished":true}""";
        using var transport = new RecordingHttpMessageHandler(request =>
        {
            var query = QueryHelpers.ParseQuery(request.Message.RequestUri?.Query);
            var response = query.ContainsKey("includePublished") && query.TryGetValue("eventId", out var id)
                ? id.ToString() switch
                {
                    eventId => published,
                    "missing-event" => retainedEmpty,
                    _ => EmptyPage,
                }
                : EmptyPage;
            return HttpResponseFactory.Json(response, HttpStatusCode.OK);
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var page = Render<OutboxDelivery>();

        // Act
        await page.Find("#outbox-include-published").ChangeAsync(new ChangeEventArgs { Value = true });
        await page.Find("form").SubmitAsync();
        var validation = page.Find("[role='alert']").TextContent;
        var requestsAfterInvalidSearch = transport.Requests.Count;
        await page.Find("#outbox-event-id").ChangeAsync(new ChangeEventArgs { Value = $" {eventId} " });
        await page.Find("form").SubmitAsync();
        var row = page.Find("tbody tr").TextContent;
        var publishedMarkup = page.Markup;
        await page.Find("#outbox-event-id").ChangeAsync(new ChangeEventArgs { Value = "missing-event" });
        await page.Find("form").SubmitAsync();
        var missing = page.Find("[role='status']").TextContent;
        await page.Find("#outbox-event-id").ChangeAsync(new ChangeEventArgs { Value = "legacy-host-example" });
        await page.Find("form").SubmitAsync();

        // Assert
        validation.ShouldBe("Enter an exact event ID to include retained published messages.");
        requestsAfterInvalidSearch.ShouldBe(1);
        row.ShouldContain(eventId, StringComparison.Ordinal);
        row.ShouldContain("Published", StringComparison.Ordinal);
        row.ShouldContain("2026-11-20 12:15:00Z", StringComparison.Ordinal);
        publishedMarkup.ShouldNotContain("synthetic-body-marker", StringComparison.Ordinal);
        missing.ShouldBe("No retained outbox message matches this search.");
        page.Find("[role='alert']").TextContent.ShouldBe("Retained outbox lookup is unavailable.");
        page.FindAll("nav[aria-label='Admin outbox pages']").ShouldBeEmpty();
        var requests = transport.Requests.Select(item => QueryHelpers.ParseQuery(item.Message.RequestUri?.Query)).ToArray();
        requests.ShouldHaveCount(4);
        requests[0].ContainsKey("includePublished").ShouldBeFalse();
        requests[1]["includePublished"].ToString().ShouldBe("true");
        requests[1]["eventId"].ToString().ShouldBe(eventId);
        requests[1].ContainsKey("afterStorageId").ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Only_the_current_retained_search_can_replace_the_outbox_results(bool earlierCompletesFirst, bool earlierLosesSession)
    {
        // Arrange
        const string retainedId = "retained-match";
        const string retained = """
            {"rows":[{"storageId":"44444444-4444-4444-4444-444444444444","eventId":"retained-match",
              "eventType":"Tours.Changed+V1","recordedAt":"2026-11-20T12:00:00Z","publishedAt":"2026-11-20T12:15:00Z",
              "attempts":0,"lastAttemptAt":null,"nextAttemptAt":null}],
              "nextRecordedAt":null,"nextStorageId":null,"includesPublished":true}
            """;
        var earlierResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var currentResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transport = new DelegateHttpMessageHandler((request, _) =>
        {
            var query = QueryHelpers.ParseQuery(request.RequestUri?.Query);
            if (query.ContainsKey("afterStorageId"))
            {
                return earlierResponse.Task;
            }

            return query.TryGetValue("eventId", out var eventId) && eventId.ToString() == retainedId
                && query.TryGetValue("includePublished", out var includePublished) && includePublished.ToString() == "true"
                ? currentResponse.Task
                : Task.FromResult(HttpResponseFactory.Json(FirstPage, HttpStatusCode.OK));
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var page = Render<OutboxDelivery>();

        // Act
        var earlierPage = page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").ClickAsync();
        await page.Find("#outbox-include-published").ChangeAsync(new ChangeEventArgs { Value = true });
        await page.Find("#outbox-event-id").ChangeAsync(new ChangeEventArgs { Value = retainedId });
        var currentSearch = page.Find("form").SubmitAsync();
        if (!earlierCompletesFirst)
        {
            currentResponse.SetResult(HttpResponseFactory.Json(retained, HttpStatusCode.OK));
            await currentSearch;
        }

        if (earlierLosesSession)
        {
            earlierResponse.SetException(new UserTokenUnavailableException("synthetic-stale-session"));
        }
        else
        {
            earlierResponse.SetResult(HttpResponseFactory.Json(LastPage, HttpStatusCode.OK));
        }

        await earlierPage;
        var interimStatuses = page.FindAll("[role='status']");
        var intermediateStatus = interimStatuses.Count == 0 ? null : interimStatuses[0].TextContent;
        var intermediateRowCount = page.FindAll("tbody tr").Count;
        if (earlierCompletesFirst)
        {
            currentResponse.SetResult(HttpResponseFactory.Json(retained, HttpStatusCode.OK));
            await currentSearch;
        }

        // Assert
        if (earlierCompletesFirst)
        {
            intermediateStatus.ShouldBe("Looking up outbox publication…");
            intermediateRowCount.ShouldBe(0);
        }

        page.FindAll("[role='alert']").ShouldBeEmpty();
        var row = page.FindAll("tbody tr").ShouldHaveSingleItem().TextContent;
        row.ShouldContain(retainedId, StringComparison.Ordinal);
        row.ShouldContain("Published", StringComparison.Ordinal);
        row.ShouldNotContain("event-pending-second", StringComparison.Ordinal);
        page.FindAll("nav[aria-label='Admin outbox pages']").ShouldBeEmpty();
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
        var page = Render<OutboxDelivery>();
        var heading = (AngleSharp.Dom.IElement)page.Find("h1").Clone();
        var initialFocus = JSInterop.Invocations[FocusInterop].Count;
        var initialRows = page.FindAll("tbody tr").Select(row => row.TextContent).ToArray();
        var initialMarkup = page.Markup;
        await page.Find("#outbox-event-type").ChangeAsync(new ChangeEventArgs { Value = " Tours.Changed+V1 " });
        await page.Find("#outbox-recorded-since").ChangeAsync(new ChangeEventArgs { Value = "2026-11-20" });
        await page.Find("form").SubmitAsync();
        await page.Find("#outbox-event-id").ChangeAsync(new ChangeEventArgs { Value = " event & detail+ " });
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
        initialRows[0].ShouldContain("Publication failed", StringComparison.Ordinal);
        initialRows[0].ShouldContain("2026-11-20 14:00:00Z", StringComparison.Ordinal);
        initialRows[1].ShouldContain("Pending publication", StringComparison.Ordinal);
        initialRows[1].ShouldNotContain("Publication failed", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("synthetic-body-marker", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("synthetic-error-marker", StringComparison.Ordinal);
        lastRow.ShouldContain("Pending publication", StringComparison.Ordinal);
        lastRow.ShouldContain("Publication failed", StringComparison.Ordinal);
        lastRow.ShouldContain("2026-11-20 12:30:00Z", StringComparison.Ordinal);
        lastNextDisabled.ShouldBeTrue();
        firstPageRowCount.ShouldBe(2);
        page.Find("[role='status']").TextContent.ShouldBe("No pending outbox messages match this search.");

        var requests = transport.Requests.Select(request => request.Message).ToArray();
        requests.ShouldHaveCount(5);
        foreach (var request in requests)
        {
            request.Method.ShouldBe(HttpMethod.Get);
            request.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/messaging/outbox");
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
    public async Task Paging_a_failed_read_keeps_focus_in_the_outbox_and_can_return_to_the_first_page()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(request =>
            QueryHelpers.ParseQuery(request.Message.RequestUri?.Query).ContainsKey("afterStorageId")
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : HttpResponseFactory.Json(FirstPage, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var page = Render<OutboxDelivery>();
        var heading = (AngleSharp.Dom.IElement)page.Find("h1").Clone();

        // Act
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").ClickAsync();
        var alert = page.Find("[role='alert']").TextContent;
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "First page").ClickAsync();

        // Assert
        alert.ShouldBe("Admin outbox metadata is unavailable.");
        var pagingFocus = JSInterop.Invocations[FocusInterop];
        pagingFocus.ShouldHaveCount(2);
        pagingFocus[0].Arguments[0].ShouldBeElementReferenceTo(heading);
        pagingFocus[1].Arguments[0].ShouldBeElementReferenceTo(heading);
        page.FindAll("tbody tr").ShouldHaveCount(2);
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
        var page = Render<OutboxDelivery>();
        var initialSnapshot = page.Find(retryRow).TextContent;

        // Act
        _clock.CurrentInstant = RetryAt.AddMinutes(elapsedMinutes);
        await page.Find("#outbox-event-type").ChangeAsync(new ChangeEventArgs { Value = "Tours.Changed+V1" });
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
        var page = Render<OutboxDelivery>();
        var failureMessage = page.Find("[role='alert']").TextContent;
        var failureMarkup = page.Markup;
        var searchDisabled = page.Find("button[type='submit']").HasAttribute("disabled");
        status = HttpStatusCode.OK;

        // Act
        await page.Find("form").SubmitAsync();

        // Assert
        failureMessage.ShouldBe("Admin outbox metadata is unavailable.");
        failureMarkup.ShouldNotContain("synthetic-private-provider-detail", StringComparison.Ordinal);
        searchDisabled.ShouldBeFalse();
        page.Find("[role='status']").TextContent.ShouldBe("No pending outbox messages match this search.");
        page.FindAll("table").ShouldBeEmpty();
        page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Next page").HasAttribute("disabled").ShouldBeTrue();
    }
}
