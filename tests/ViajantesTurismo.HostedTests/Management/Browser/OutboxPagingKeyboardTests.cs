using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Drives outbox navigation with keyboard input on the real Management Web page.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class OutboxPagingKeyboardTests : ManagementWebBrowserTestBase
{
    private readonly OutboxPagingBrowserScenario _scenario = new();

    public OutboxPagingKeyboardTests()
    {
        Registrations.AddHttpClient<IMessagingHealthApiClient, MessagingHealthApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => _scenario);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _scenario.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Keyboard_focus_stays_on_the_outbox_heading_when_a_pending_page_fails_and_the_first_page_recovers()
    {
        // Arrange
        await NavigateTo("/operations/messaging/outbox");
        var main = Page.GetByRole(AriaRole.Main);
        var heading = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Admin outbox", Level = 1 });
        var next = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next page", Exact = true });
        var first = main.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "First page", Exact = true });
        var initialRow = main.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = "synthetic-pending-publication" });
        await Expect(initialRow).ToBeVisibleAsync();
        await next.FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main.GetByRole(AriaRole.Status)).ToHaveTextAsync("Loading pending outbox messages…");
        await Expect(heading).ToBeFocusedAsync();

        // Act
        _scenario.AllowContinuation();

        // Assert
        await Expect(main.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Admin outbox metadata is unavailable.");
        await Expect(heading).ToBeFocusedAsync();

        // Act
        await first.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(initialRow).ToBeVisibleAsync();
        await Expect(first).ToBeDisabledAsync();
        await Expect(heading).ToBeFocusedAsync();
    }
}
