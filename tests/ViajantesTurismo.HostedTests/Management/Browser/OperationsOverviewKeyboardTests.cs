using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Drives the operations overview, refresh and drill-down links in Chromium.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class OperationsOverviewKeyboardTests : ManagementWebBrowserTestBase
{
    private const string Maintenance = """
        {"schedules":[
          {"job":"Drafts","isPaused":false,"activeRun":{"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-20T11:30:00Z","status":"Running"}},
          {"job":"Audit","isPaused":false,"latestRun":{"runId":"22222222-2222-2222-2222-222222222222","startedAtUtc":"2026-11-20T10:30:00Z",
            "finishedAtUtc":"2026-11-20T10:45:00Z","status":"Failed"}},
          {"job":"Idempotency","isPaused":false}
        ],"draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
        """;

    private const string Messaging = """
        {
          "observedAtUtc":"2026-11-20T12:00:00Z","catalogTerminalDeliveryFailureCount":0,"catalogOldestTerminalFailureAt":null,
          "catalogRetryCount":0,"catalogOldestRetryReceivedAt":null,"catalogDueRetryCount":0
        }
        """;

    private const string Inbox = """
        {"entriesWithDuplicates":0,"duplicateCount":0,"lastObservedAt":null}
        """;

    private const string UpdatedMessaging = """
        {
          "observedAtUtc":"2026-11-20T12:00:00Z","adminPendingOutboxCount":2,
          "adminOldestPendingOutboxEnqueuedAt":"2026-11-20T08:00:00Z",
          "catalogTerminalDeliveryFailureCount":1,"catalogOldestTerminalFailureAt":"2026-11-20T11:00:00Z",
          "catalogRetryCount":1,"catalogOldestRetryReceivedAt":"2026-11-20T10:00:00Z","catalogDueRetryCount":1
        }
        """;

    private const string CatalogDelivery = """
        {
          "rows":[{
            "storageId":"11111111-1111-1111-1111-111111111111","eventId":"synthetic-retry",
            "eventType":"TourChanged","recordedAt":"2026-11-20T10:00:00Z","attempts":2,
            "lastAttemptAt":"2026-11-20T11:00:00Z","nextAttemptAt":"2026-11-20T12:00:00Z","terminalAt":null
          }],
          "nextRecordedAt":null,"nextStorageId":null
        }
        """;

    private const string OutboxDelivery = """
        {
          "rows":[{
            "storageId":"22222222-2222-2222-2222-222222222222","eventId":"synthetic-pending-publication",
            "eventType":"TourChanged","recordedAt":"2026-11-20T08:00:00Z","attempts":2,
            "lastAttemptAt":"2026-11-20T09:00:00Z","nextAttemptAt":"2026-11-20T10:00:00Z"
          }],
          "nextRecordedAt":null,"nextStorageId":null
        }
        """;

    private string _messagingSnapshot = Messaging;
    private readonly DocumentMaintenanceTimelineBrowserScenario _timeline = new();
    private readonly OperationsOverviewRefreshGate _refresh;

    public OperationsOverviewKeyboardTests()
    {
        _refresh = new OperationsOverviewRefreshGate(request =>
            HttpResponseFactory.Json(request.RequestUri?.AbsolutePath switch
            {
                "/api/v1/operations/messaging/catalog-delivery" => CatalogDelivery,
                "/api/v1/operations/messaging/outbox" => OutboxDelivery,
                _ => Volatile.Read(ref _messagingSnapshot)
            }, HttpStatusCode.OK));

        Registrations.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)));
        Registrations.AddHttpClient<IDocumentMaintenanceApiClient, DocumentMaintenanceApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(Maintenance, HttpStatusCode.OK)));
        Registrations.AddSingleton<IDocumentMaintenanceTimelineApiClient>(_timeline);
        Registrations.AddHttpClient<ICatalogInboxOperationsApiClient, CatalogInboxOperationsApiClient>(
                client => client.BaseAddress = new Uri("https://catalog.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(Inbox, HttpStatusCode.OK)));
        Registrations.AddHttpClient<ICatalogGalleryOperationsApiClient, CatalogGalleryOperationsApiClient>(
                client => client.BaseAddress = new Uri("https://catalog.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(_ =>
                HttpResponseFactory.Json("""{"duePlacementCount":0,"oldestDueAt":null}""", HttpStatusCode.OK)));
        Registrations.AddHttpClient<IMessagingHealthApiClient, MessagingHealthApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => _refresh);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _refresh.Dispose();
        _timeline.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Keyboard_pages_through_retained_batches_and_returns_to_the_schedule_overview()
    {
        // Arrange
        await NavigateTo("/operations");
        var main = Page.GetByRole(AriaRole.Main);
        var scheduled = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Scheduled jobs" });
        var maintenance = scheduled.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View document maintenance" });
        var draft = main.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Draft retention timeline" });
        var timeline = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Draft retention timeline", Level = 1 });
        var back = main.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Back to document maintenance" });
        var latest = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Latest page" });
        var earlier = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Earlier page" });
        var batches = main.GetByRole(AriaRole.Table, new LocatorGetByRoleOptions { Name = "Recorded batches" });
        await Expect(maintenance).ToBeVisibleAsync();
        await main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Refresh" }).FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(maintenance).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(draft).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(draft).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(batches).ToContainTextAsync("2026-11-20 12:00:00Z");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(back).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(earlier).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(main.GetByRole(AriaRole.Status)).ToContainTextAsync("Loading recorded batches");
        await Expect(timeline).ToBeFocusedAsync();
        _timeline.AllowEarlierPage();

        // Assert
        await Expect(batches).ToContainTextAsync("2026-11-20 08:00:00Z");
        await Expect(earlier).ToBeDisabledAsync();
        await Expect(timeline).ToBeFocusedAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(back).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(latest).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(batches).ToContainTextAsync("2026-11-20 12:00:00Z");
        await Expect(latest).ToBeDisabledAsync();
        await Expect(timeline).ToBeFocusedAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(back).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Document maintenance", Level = 1 }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Operations", Exact = true }).PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Operations", Level = 1 }))
            .ToBeVisibleAsync();
        await Expect(maintenance).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Tab_reaches_the_running_and_failed_job_links_and_enter_opens_the_timelines()
    {
        // Arrange
        await NavigateTo("/operations");
        var main = Page.GetByRole(AriaRole.Main);
        var refresh = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Refresh" });
        var scheduled = main.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View document maintenance" });
        var runningCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Running jobs" });
        var failedCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Failed jobs" });
        var running = runningCard.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View running job timelines" });
        var failed = failedCard.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View failed job timelines" });
        await Expect(runningCard).ToContainTextAsync("0h 30m");
        await Expect(failedCard).ToContainTextAsync("1h 15m");
        await refresh.FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(scheduled).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(running).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(failed).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Document maintenance", Level = 1 }))
            .ToBeVisibleAsync();
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Draft retention" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Refresh_keeps_keyboard_focus_while_loading_and_updates_the_catalog_metadata()
    {
        // Arrange
        await NavigateTo("/operations");
        var main = Page.GetByRole(AriaRole.Main);
        var refresh = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Refresh" });
        var retryCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Catalog delivery retries" });
        var terminalCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Catalog terminal failures" });
        var outboxCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Pending outbox" });
        await Expect(retryCard).ToContainTextAsync("No scheduled Catalog delivery retries.");
        await refresh.FocusAsync();
        Volatile.Write(ref _messagingSnapshot, UpdatedMessaging);
        _refresh.HoldRefresh();

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Status)).ToHaveTextAsync("Loading operations health…");
        await Expect(refresh).ToBeDisabledAsync();
        await Expect(refresh).ToBeFocusedAsync();

        // Act
        await Page.Keyboard.PressAsync("Enter");
        _refresh.AllowRefresh();

        // Assert
        await Expect(retryCard).ToContainTextAsync("2h 0m");
        await Expect(terminalCard).ToContainTextAsync("1h 0m");
        await Expect(outboxCard).ToContainTextAsync("4h 0m");
        await Expect(refresh).ToBeEnabledAsync();
        await Expect(refresh).ToBeFocusedAsync();

        // Act
        var retryLink = retryCard.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Inspect Catalog deliveries" });
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(retryLink).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Catalog deliveries", Level = 1 }))
            .ToBeVisibleAsync();
        var retryRow = main.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = "synthetic-retry" });
        await Expect(retryRow).ToContainTextAsync("Retry due");
        await Expect(retryRow).ToContainTextAsync("Consumption failed");
        _refresh.RefreshRequests.ShouldBe(1);
    }

    [Fact]
    public async Task Tab_reaches_the_pending_outbox_link_and_enter_opens_publication_metadata()
    {
        // Arrange
        Volatile.Write(ref _messagingSnapshot, UpdatedMessaging);
        await NavigateTo("/operations");
        var main = Page.GetByRole(AriaRole.Main);
        var refresh = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Refresh" });
        var outboxCard = main.GetByRole(AriaRole.Region, new LocatorGetByRoleOptions { Name = "Pending outbox" });
        var details = outboxCard.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Inspect pending outbox" });
        await Expect(details).ToBeVisibleAsync();
        await refresh.FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(details).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Admin outbox", Level = 1 }))
            .ToBeVisibleAsync();
        var row = main.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = "synthetic-pending-publication" });
        await Expect(row).ToContainTextAsync("Retry due");
        await Expect(row).ToContainTextAsync("Publication failed");
        await Expect(row.GetByRole(AriaRole.Cell, new LocatorGetByRoleOptions { Name = "2", Exact = true })).ToBeVisibleAsync();
    }
}
