using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Drives the document maintenance timeline and its keyboard focus in Chromium.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class DocumentMaintenanceTimelineKeyboardTests : ManagementWebBrowserTestBase
{
    private const string Maintenance = """
        {"schedules":[{"job":"Drafts","isPaused":false},{"job":"Audit","isPaused":false},{"job":"Idempotency","isPaused":false}],
          "draftRuns":[],"auditRuns":[],"idempotencyRuns":[]}
        """;

    private readonly DocumentMaintenanceTimelineBrowserScenario _timeline = new();

    public DocumentMaintenanceTimelineKeyboardTests()
    {
        Registrations.AddHttpClient<IDocumentMaintenanceApiClient, DocumentMaintenanceApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(Maintenance, HttpStatusCode.OK)));
        Registrations.AddSingleton<IDocumentMaintenanceTimelineApiClient>(_timeline);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _timeline.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Keyboard_pages_through_retained_batches_and_returns_to_document_maintenance()
    {
        // Arrange
        await NavigateTo("/operations/document-maintenance");
        var main = Page.GetByRole(AriaRole.Main);
        var overview = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Document maintenance", Level = 1 });
        var draft = main.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Draft retention timeline" });
        var timeline = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Draft retention timeline", Level = 1 });
        var back = main.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Back to document maintenance" });
        var latest = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Latest page" });
        var earlier = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Earlier page" });
        var batches = main.GetByRole(AriaRole.Table, new LocatorGetByRoleOptions { Name = "Recorded batches" });
        await Expect(overview).ToBeVisibleAsync();
        await draft.FocusAsync();

        // Act
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

        // Assert
        await Expect(overview).ToBeVisibleAsync();
        await Expect(draft).ToBeVisibleAsync();
    }
}
