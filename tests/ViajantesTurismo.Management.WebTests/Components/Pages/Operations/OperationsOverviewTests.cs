using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;
using ViajantesTurismo.Management.WebTests.Services;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class OperationsOverviewTests : BunitContext
{
    private const string UnregisteredMaintenance = """
        {"schedules":[{"job":"Drafts","isPaused":null},{"job":"Audit","isPaused":null},{"job":"Idempotency","isPaused":null}],
         "draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
        """;

    private const string EmptyMessaging = """
        {
          "observedAtUtc":"2026-11-20T12:00:00Z","catalogTerminalDeliveryFailureCount":0,"catalogOldestTerminalFailureAt":null,
          "catalogRetryCount":0,"catalogOldestRetryReceivedAt":null,"catalogDueRetryCount":0
        }
        """;

    public OperationsOverviewTests()
    {
        Services.AddOptions<OperationsDashboardThresholds>();
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)));
        Services.AddHttpClient<IDocumentMaintenanceApiClient, DocumentMaintenanceApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() =>
                new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(UnregisteredMaintenance, HttpStatusCode.OK)));
        Services.AddHttpClient<ICatalogInboxOperationsApiClient, CatalogInboxOperationsApiClient>(
                client => client.BaseAddress = new Uri("https://catalog.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() =>
                new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json("{}", HttpStatusCode.ServiceUnavailable)));
        Services.AddHttpClient<ICatalogGalleryOperationsApiClient, CatalogGalleryOperationsApiClient>(
                client => client.BaseAddress = new Uri("https://catalog.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() =>
                new RecordingHttpMessageHandler(HttpStatusCode.ServiceUnavailable));
        Services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment(string.Empty));
    }

    [Theory]
    [InlineData(true, "1h 45m")]
    [InlineData(false, "Unavailable")]
    public void Overview_combines_document_schedule_and_delivery_evidence_with_clear_unknowns(bool hasTerminalCompletion, string oldestFailedAge)
    {
        // Arrange
        var terminalCompletion = hasTerminalCompletion ? "\"2026-11-20T10:15:00Z\"" : "null";
        var maintenance = $$$"""
            {"schedules":[
              {"job":"Drafts","isPaused":true,"nextDueAtUtc":"2026-11-20T08:00:00Z","isDue":false,"activeRun":{"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-20T11:30:00Z","status":"Running"}},
              {"job":"Audit","isPaused":false,"nextDueAtUtc":"2026-11-20T11:00:00Z","isDue":true,"latestRun":{"runId":"22222222-2222-2222-2222-222222222222","startedAtUtc":"2026-11-20T10:30:00Z",
                "finishedAtUtc":"2026-11-20T10:45:00Z","status":"Failed"}},
              {"job":"Idempotency","isPaused":false,"nextDueAtUtc":"2026-11-20T09:30:00Z","isDue":true,"latestRun":{"runId":"44444444-4444-4444-4444-444444444444","startedAtUtc":"2026-11-20T10:00:00Z",
                "finishedAtUtc":{{{terminalCompletion}}},"status":"TerminalFailure"}}
            ],"draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
            """;
        const string messaging = """
            {
              "observedAtUtc":"2026-11-20T12:00:00Z",
              "adminPendingOutboxCount":3,
              "adminOldestPendingOutboxEnqueuedAt":"2026-11-20T08:00:00Z",
              "catalogTerminalDeliveryFailureCount":2,
              "catalogOldestTerminalFailureAt":"2026-11-20T09:00:00Z",
              "catalogRetryCount":1,
              "catalogOldestRetryReceivedAt":"2026-11-19T11:30:00Z",
              "catalogDueRetryCount":1
            }
            """;
        using var transport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance" ? maintenance : messaging, HttpStatusCode.OK));
        using var client = new HttpClient(transport)
        {
            BaseAddress = new Uri("https://admin.example.test"),
        };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();

        // Assert
        component.FindAll("section[aria-labelledby]").Count.ShouldBe(8);
        var scheduled = component.FindAll("[aria-labelledby='scheduled-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        scheduled.ShouldBe(["3", "2", "2h 30m", "Not configured"]);
        component.Find("[aria-labelledby='scheduled-jobs'] time").GetAttribute("datetime").ShouldBe("2026-11-20T09:30:00.0000000+00:00");
        var running = component.FindAll("[aria-labelledby='running-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        var failed = component.FindAll("[aria-labelledby='failed-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        running.ShouldBe(["1", "0h 30m", "Not configured"]);
        failed.ShouldBe(["2", oldestFailedAge, "Not configured"]);
        component.Find("[aria-labelledby='running-jobs'] a").GetAttribute("href").ShouldBe("operations/document-maintenance");
        component.Find("[aria-labelledby='failed-jobs'] a").GetAttribute("href").ShouldBe("operations/document-maintenance");
        foreach (var heading in new[] { "idempotency-duplicates", "stuck-sagas" })
        {
            component.Find($"[aria-labelledby='{heading}'] dd").TextContent.ShouldBe("Unavailable");
        }

        component.Find("[aria-labelledby='pending-outbox'] dd").TextContent.ShouldBe("3");
        component.Find("[aria-labelledby='pending-outbox'] time").TextContent.ShouldBe("4h 0m");
        component.Find("[aria-labelledby='pending-outbox'] a").GetAttribute("href")
            .ShouldBe("operations/messaging/outbox");
        component.Find("[aria-labelledby='catalog-terminal-failures'] time").TextContent.ShouldBe("3h 0m");
        component.Find("[aria-labelledby='catalog-retry-backlog'] time").TextContent.ShouldBe("1d 0h 30m");
        component.Find("[aria-labelledby='catalog-retry-backlog'] p").TextContent.ShouldBe("1 due at snapshot time.");
        component.Find("[aria-labelledby='scheduled-jobs'] a").TextContent.ShouldBe("View document maintenance");
        component.Find("[aria-labelledby='catalog-retry-backlog'] a").GetAttribute("href")
            .ShouldBe("operations/messaging/catalog-delivery");
        component.Find("[aria-labelledby='catalog-terminal-failures'] a").GetAttribute("href")
            .ShouldBe("operations/messaging/catalog-delivery");
        component.Markup.ShouldContain("Test environment · Read-only", StringComparison.Ordinal);
        transport.Requests.ShouldHaveCount(2);
        transport.Requests.Select(request => request.Method).ToArray().ShouldBe([HttpMethod.Get, HttpMethod.Get]);
    }

    [Theory]
    [InlineData(0, true, "None")]
    [InlineData(3, false, "Unavailable")]
    public void Shows_explicit_empty_counts_threshold_availability_and_unknown_ages(int count, bool empty, string age)
    {
        // Arrange
        var payload = $$"""
            {
              "observedAtUtc":"2026-11-20T12:00:00Z","adminPendingOutboxCount":0,"adminOldestPendingOutboxEnqueuedAt":null,
              "catalogTerminalDeliveryFailureCount":{{count}},"catalogOldestTerminalFailureAt":null,
              "catalogRetryCount":0,"catalogOldestRetryReceivedAt":null,"catalogDueRetryCount":0
            }
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(payload, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();

        // Assert
        var statistics = component.FindAll("[aria-labelledby='catalog-terminal-failures'] dd").Select(item => item.TextContent.Trim()).ToArray();
        statistics.ShouldBe([count.ToString(CultureInfo.InvariantCulture), age, "Not configured"]);
        component.Markup.Contains("No Catalog terminal failures.", StringComparison.Ordinal).ShouldBe(empty);
        component.Markup.ShouldContain("No scheduled Catalog delivery retries.", StringComparison.Ordinal);
        component.Find("[aria-labelledby='pending-outbox'] dd").TextContent.ShouldBe("0");
        var running = component.FindAll("[aria-labelledby='running-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        var failed = component.FindAll("[aria-labelledby='failed-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        running.ShouldBe(["0", "None", "Not configured"]);
        failed.ShouldBe(["0", "None", "Not configured"]);
        component.Find("[aria-labelledby='running-jobs']").TextContent.ShouldContain("No active document maintenance runs.", StringComparison.Ordinal);
        component.Find("[aria-labelledby='failed-jobs']").TextContent.ShouldContain("No latest document maintenance runs failed.", StringComparison.Ordinal);
        component.Markup.ShouldContain("No Admin outbox messages are pending.", StringComparison.Ordinal);
        var scheduled = component.FindAll("[aria-labelledby='scheduled-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        scheduled.ShouldBe(["0", "0", "None", "Not configured"]);
        component.Markup.ShouldContain("No document maintenance schedule is registered.", StringComparison.Ordinal);
        var request = transport.Requests.ShouldHaveSingleItem();
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/messaging");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Gallery_retry_health_preserves_empty_unavailable_and_populated_states(HttpStatusCode unavailableStatus)
    {
        // Arrange
        const string card = "[aria-labelledby='stuck-sagas']";
        const string emptyMessage = "No Gallery placements have repeated retries due.";
        var body = """{"duePlacementCount":0,"oldestDueAt":null}""";
        var status = HttpStatusCode.OK;
        using var catalogTransport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, status));
        using var catalogClient = new HttpClient(catalogTransport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(catalogClient));
        using var adminTransport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyMessaging, HttpStatusCode.OK));
        using var adminClient = new HttpClient(adminTransport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(adminClient));

        // Act
        var overview = Render<OperationsOverview>();
        var empty = overview.FindAll($"{card} dd").Select(metric => metric.TextContent.Trim()).ToArray();
        var emptyMarkup = overview.Find(card).TextContent;
        status = unavailableStatus;
        await overview.Find("button").ClickAsync();
        var unavailable = overview.FindAll($"{card} dd").Select(metric => metric.TextContent.Trim()).ToArray();
        var unavailableMarkup = overview.Find(card).TextContent;
        body = """{"duePlacementCount":2,"oldestDueAt":"2026-11-20T10:15:00Z","payload":"synthetic-private-event"}""";
        status = HttpStatusCode.OK;
        await overview.Find("button").ClickAsync();

        // Assert
        empty.ShouldBe(["0", "None", "Not configured"]);
        emptyMarkup.ShouldContain(emptyMessage, StringComparison.Ordinal);
        unavailable.ShouldBe(["Unavailable", "Unavailable", "Unavailable"]);
        unavailableMarkup.ShouldNotContain(emptyMessage, StringComparison.Ordinal);
        var metrics = overview.FindAll($"{card} dd").Select(metric => metric.TextContent.Trim()).ToArray();
        metrics.ShouldBe(["2", "1h 45m", "Not configured"]);
        overview.Find($"{card} time").GetAttribute("datetime").ShouldBe("2026-11-20T10:15:00.0000000+00:00");
        overview.Find($"{card} a").GetAttribute("href").ShouldBe("operations/gallery");
        overview.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent.ShouldBe("0");
        overview.Markup.ShouldNotContain("synthetic-private-event", StringComparison.Ordinal);
    }

    [Fact]
    public void The_duplicate_card_opens_the_bounded_inbox_on_arrival()
    {
        // Arrange
        const string duplicateEventId = "33333333-3333-3333-3333-333333333333";
        const string inbox = "/api/v1/catalog/operations/messaging/inbox";
        using var adminTransport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/messaging/catalog-delivery"
                ? """{"rows":[],"nextRecordedAt":null,"nextStorageId":null}""" : EmptyMessaging,
            HttpStatusCode.OK));
        using var catalogTransport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == inbox
                ? """{"entriesWithDuplicates":1,"duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z"}"""
                : $$"""{"rows":[{"eventId":"{{duplicateEventId}}","duplicateCount":2,"lastObservedAt":"2026-11-24T09:20:00Z","payload":"synthetic-secret"}],"nextObservedAt":null,"nextEventId":null}""",
            HttpStatusCode.OK));
        using var adminClient = new HttpClient(adminTransport) { BaseAddress = new Uri("https://admin.example.test") };
        using var catalogClient = new HttpClient(catalogTransport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(adminClient));
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(catalogClient));
        Services.AddSingleton(TimeProvider.System);
        var overview = Render<OperationsOverview>();
        var link = overview.Find("[aria-labelledby='idempotency-duplicates'] a").GetAttribute("href");

        // Act
        this.GetNavigationManager().NavigateTo(link.ShouldNotBeNull());
        var deliveries = Render<CatalogDelivery>();

        // Assert
        deliveries.Markup.ShouldContain(duplicateEventId, StringComparison.Ordinal);
        deliveries.Markup.ShouldNotContain("synthetic-secret", StringComparison.Ordinal);
        var requests = catalogTransport.Requests;
        requests.Select(request => request.Method).ToArray().ShouldBe([HttpMethod.Get, HttpMethod.Get, HttpMethod.Get]);
        requests.Select(request => request.Message.RequestUri?.PathAndQuery).ToArray()
            .ShouldBe([inbox, inbox, $"{inbox}/duplicates?limit=25"]);
    }

    [Theory]
    [InlineData(2, true, "Above threshold")]
    [InlineData(3, true, "Within threshold")]
    [InlineData(0, false, "Unavailable")]
    public void Shows_configured_count_thresholds_only_for_available_measurements(
        long maximumPending,
        bool messagingAvailable,
        string pendingStatus)
    {
        // Arrange
        const string maintenance = """
            {"schedules":[
              {"job":"Drafts","isPaused":false,"nextDueAtUtc":"2026-11-20T11:00:00Z","isDue":true},
              {"job":"Audit","isPaused":true,"nextDueAtUtc":"2026-11-20T10:00:00Z","isDue":false}
            ],"draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
            """;
        const string messaging = """
            {
              "observedAtUtc":"2026-11-20T12:00:00Z","adminPendingOutboxCount":3,
              "catalogTerminalDeliveryFailureCount":0,"catalogOldestTerminalFailureAt":null,
              "catalogRetryCount":0,"catalogOldestRetryReceivedAt":null,"catalogDueRetryCount":0
            }
            """;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Operations:Dashboard:MaximumCounts:scheduled-jobs"] = "1",
            ["Operations:Dashboard:MaximumCounts:pending-outbox"] = maximumPending.ToString(CultureInfo.InvariantCulture),
        }).Build();
        Services.AddSingleton<IConfiguration>(configuration);
        OperationsDashboardThresholds.AddOptions(Services);
        using var transport = new RecordingHttpMessageHandler(request =>
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance"
                ? HttpResponseFactory.Json(maintenance, HttpStatusCode.OK)
                : HttpResponseFactory.Json(messaging, messagingAvailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();

        // Assert
        component.Find("[aria-labelledby='scheduled-jobs'] dd:last-of-type").TextContent.ShouldBe("Within threshold");
        component.Find("[aria-labelledby='pending-outbox'] dd:last-of-type").TextContent.ShouldBe(pendingStatus);
        component.Find("[aria-labelledby='failed-jobs'] dd:last-of-type").TextContent.ShouldBe("Not configured");
    }

    [Fact]
    public void Rejects_negative_count_thresholds()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Operations:Dashboard:MaximumCounts:pending-outbox"] = "-1",
        }).Build();
        Services.AddSingleton<IConfiguration>(configuration);
        OperationsDashboardThresholds.AddOptions(Services);
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyMessaging, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var renderOverview = () => Render<OperationsOverview>();

        // Assert
        renderOverview.ShouldThrow<OptionsValidationException>().Message.ShouldContain("must be non-negative", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shows_loading_and_disables_refresh_until_the_reads_finish()
    {
        // Arrange
        using var responseHandler = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyMessaging, HttpStatusCode.OK));
        using var transport = new GatedHttpMessageHandler(responseHandler);
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();
        await transport.WaitForRequests(1, Xunit.TestContext.Current.CancellationToken);
        var status = component.Find("[role='status']").TextContent;
        var refreshDisabled = component.Find("button").GetAttribute("aria-disabled");
        transport.Release();

        // Assert
        status.ShouldBe("Loading operations health…");
        refreshDisabled.ShouldBe("true");
        component.WaitForAssertion(() => component.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent.ShouldBe("0"));
        component.Find("button").GetAttribute("aria-disabled").ShouldBe("false");
    }

    [Fact]
    public async Task A_messaging_error_keeps_schedule_status_visible_and_refresh_can_recover()
    {
        // Arrange
        var status = HttpStatusCode.ServiceUnavailable;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyMessaging, status));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        var component = Render<OperationsOverview>();
        var error = component.Find("[role='alert']").TextContent;
        var maintenanceCount = component.Find("[aria-labelledby='scheduled-jobs'] dd").TextContent;
        var unavailableCount = component.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent;
        status = HttpStatusCode.OK;

        // Act
        await component.Find("button").ClickAsync();

        // Assert
        error.ShouldBe("Messaging health is unavailable.");
        maintenanceCount.ShouldBe("0");
        unavailableCount.ShouldBe("Unavailable");
        component.FindAll("[role='alert']").ShouldBeEmpty();
        component.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent.ShouldBe("0");
        transport.Requests.ShouldHaveCount(2);
    }

    [Fact]
    public void A_schedule_error_keeps_delivery_health_visible()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(request =>
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance"
                ? HttpResponseFactory.Json("{}", HttpStatusCode.ServiceUnavailable)
                : HttpResponseFactory.Json(EmptyMessaging, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();

        // Assert
        component.Find("[role='alert']").TextContent.ShouldBe("Document maintenance status is unavailable.");
        var scheduled = component.FindAll("[aria-labelledby='scheduled-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        scheduled.ShouldBe(["Unavailable", "Unavailable", "Unavailable", "Unavailable"]);
        var running = component.FindAll("[aria-labelledby='running-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        var failed = component.FindAll("[aria-labelledby='failed-jobs'] dd").Select(item => item.TextContent.Trim()).ToArray();
        running.ShouldBe(["Unavailable", "Unavailable", "Unavailable"]);
        failed.ShouldBe(["Unavailable", "Unavailable", "Unavailable"]);
        component.Find("[aria-labelledby='pending-outbox'] dd").TextContent.ShouldBe("Unavailable");
        component.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent.ShouldBe("0");
    }

    [Theory]
    [InlineData(true, "Document maintenance status is unavailable.", "scheduled-jobs", "catalog-terminal-failures")]
    [InlineData(false, "Messaging health is unavailable.", "catalog-terminal-failures", "scheduled-jobs")]
    public async Task An_unavailable_user_token_preserves_the_other_read_and_allows_refresh(
        bool maintenanceFails,
        string errorMessage,
        string unavailableHeading,
        string availableHeading)
    {
        // Arrange
        const string privateFailure = "synthetic-private-session-detail";
        var tokenUnavailable = true;
        using var transport = new RecordingHttpMessageHandler(request =>
        {
            var maintenance = request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance";
            if (tokenUnavailable && maintenance == maintenanceFails)
            {
                throw new UserTokenUnavailableException(privateFailure);
            }

            return HttpResponseFactory.Json(maintenance ? UnregisteredMaintenance : EmptyMessaging, HttpStatusCode.OK);
        });
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var component = Render<OperationsOverview>();
        var error = component.Find("[role='alert']").TextContent;
        var failedCount = component.Find($"[aria-labelledby='{unavailableHeading}'] dd").TextContent;
        var healthyCount = component.Find($"[aria-labelledby='{availableHeading}'] dd").TextContent;
        var hadPrivateFailure = component.Markup.Contains(privateFailure, StringComparison.Ordinal);
        var refresh = component.Find("button");
        var refreshDisabled = refresh.GetAttribute("aria-disabled");
        tokenUnavailable = false;
        await refresh.ClickAsync();

        // Assert
        error.ShouldBe(errorMessage);
        failedCount.ShouldBe("Unavailable");
        healthyCount.ShouldBe("0");
        hadPrivateFailure.ShouldBeFalse();
        refreshDisabled.ShouldBe("false");
        component.FindAll("[role='alert']").ShouldBeEmpty();
        component.Find($"[aria-labelledby='{unavailableHeading}'] dd").TextContent.ShouldBe("0");
        transport.Requests.ShouldHaveCount(4);
    }

    [Fact]
    public async Task Shows_retained_duplicates_and_recovers_from_an_unavailable_token()
    {
        // Arrange
        const string privateFailure = "synthetic-private-session-detail";
        const string emptyInbox = """
            {"entriesWithDuplicates":0,"duplicateCount":0,"lastObservedAt":null}
            """;
        const string activeInbox = """
            {"entriesWithDuplicates":2,"duplicateCount":5,"lastObservedAt":"2026-11-19T09:20:00Z","payload":"synthetic-private-payload"}
            """;
        const string duplicateCard = "[aria-labelledby='idempotency-duplicates']";
        var inbox = emptyInbox;
        var tokenUnavailable = false;
        using var transport = new RecordingHttpMessageHandler(_ => tokenUnavailable
            ? throw new UserTokenUnavailableException(privateFailure)
            : HttpResponseFactory.Json(inbox, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogInboxOperationsApiClient>(new CatalogInboxOperationsApiClient(client));
        using var messagingTransport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(EmptyMessaging, HttpStatusCode.OK));
        using var messagingClient = new HttpClient(messagingTransport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(messagingClient));

        // Act
        var component = Render<OperationsOverview>();
        var emptyMetrics = component.FindAll($"{duplicateCard} dd").Select(item => item.TextContent.Trim()).ToArray();
        var emptyMarkup = component.Markup;
        tokenUnavailable = true;
        await component.Find("button").ClickAsync();
        var unavailableMetrics = component.FindAll($"{duplicateCard} dd").Select(item => item.TextContent.Trim()).ToArray();
        var retainedSchedule = component.Find("[aria-labelledby='scheduled-jobs'] dd").TextContent;
        var retainedMessaging = component.Find("[aria-labelledby='catalog-terminal-failures'] dd").TextContent;
        var unavailableMarkup = component.Markup;
        tokenUnavailable = false;
        inbox = activeInbox;
        await component.Find("button").ClickAsync();

        // Assert
        emptyMetrics.ShouldBe(["0", "None", "Not configured"]);
        emptyMarkup.ShouldContain("No completed Catalog inbox duplicates.", StringComparison.Ordinal);
        unavailableMetrics.ShouldBe(["Unavailable", "Unavailable", "Unavailable"]);
        retainedSchedule.ShouldBe("0");
        retainedMessaging.ShouldBe("0");
        unavailableMarkup.ShouldNotContain(privateFailure, StringComparison.Ordinal);
        component.Find($"{duplicateCard} dd").TextContent.ShouldBe("5");
        var metricLabels = component.FindAll($"{duplicateCard} dt").Select(item => item.TextContent.Trim()).ToArray();
        metricLabels.ShouldBe(["Count", "Latest completed duplicate age", "Count threshold"]);
        var duplicateTime = component.Find($"{duplicateCard} time");
        duplicateTime.TextContent.ShouldBe("1d 2h 40m");
        duplicateTime.GetAttribute("datetime").ShouldBe("2026-11-19T09:20:00.0000000+00:00");
        component.Find("[aria-labelledby='idempotency-duplicates'] a").GetAttribute("href")
            .ShouldBe("operations/messaging/catalog-delivery?view=inbox#catalog-inbox-heading");
        component.Markup.ShouldNotContain("synthetic-private-payload", StringComparison.Ordinal);
        transport.Requests.Select(request => request.Message.RequestUri?.AbsolutePath).ToArray().ShouldBe([
            "/api/v1/catalog/operations/messaging/inbox", "/api/v1/catalog/operations/messaging/inbox", "/api/v1/catalog/operations/messaging/inbox"]);
        transport.Requests.All(request => request.Method == HttpMethod.Get).ShouldBeTrue();
    }

    [Fact]
    public void Failed_jobs_counts_the_latest_retryable_and_terminal_failures()
    {
        // Arrange
        const string maintenance = """
            {"schedules":[
              {"job":"Drafts","isPaused":false,"latestRun":{"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-20T11:30:00Z","status":"Failed"}},
              {"job":"Audit","isPaused":false,"latestRun":{"runId":"22222222-2222-2222-2222-222222222222","startedAtUtc":"2026-11-20T10:30:00Z","status":"TerminalFailure"}},
              {"job":"Idempotency","isPaused":false,"latestRun":{"runId":"33333333-3333-3333-3333-333333333333","startedAtUtc":"2026-11-20T09:30:00Z","status":"Succeeded"}}
            ],"draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
            """;
        using var transport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance" ? maintenance : EmptyMessaging, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));

        // Act
        var overview = Render<OperationsOverview>();

        // Assert
        overview.Find("[aria-labelledby='failed-jobs'] dd").TextContent.ShouldBe("2");
        overview.Find("[aria-labelledby='running-jobs'] dd").TextContent.ShouldBe("0");
    }

    [Theory]
    [InlineData("Unknown", true)]
    [InlineData("Succeeded", false)]
    public void Reports_a_latest_unrecorded_outcome_alongside_the_measured_failure_count(string status, bool hasUnrecordedOutcome)
    {
        // Arrange
        var maintenance = $$"""
            {"schedules":[
              {"job":"Drafts","isPaused":false,"latestRun":{"runId":"11111111-1111-1111-1111-111111111111",
                "startedAtUtc":"2026-11-20T11:30:00Z","status":"{{status}}"
              }
            }
            ],"draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
            """;
        using var transport = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath == "/api/v1/operations/document-maintenance" ? maintenance : EmptyMessaging, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));
        Services.AddSingleton<IMessagingHealthApiClient>(new MessagingHealthApiClient(client));
        Services.Configure<OperationsDashboardThresholds>(options => options.MaximumCounts["failed-jobs"] = 0);

        // Act
        var overview = Render<OperationsOverview>();
        const string failedCard = "[aria-labelledby='failed-jobs']";
        var unrecordedCounts = overview.FindAll($"{failedCard} dt")
            .Where(term => term.TextContent == "Latest outcomes unrecorded")
            .Select(term => term.NextElementSibling!.TextContent)
            .ToArray();

        // Assert
        overview.Find($"{failedCard} dd").TextContent.ShouldBe("0");
        overview.Find(failedCard).TextContent.ShouldContain("Within threshold", StringComparison.Ordinal);
        unrecordedCounts.ShouldBe(hasUnrecordedOutcome ? ["1"] : []);
    }
}
