using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>Browser keyboard journey for cancelling a held customer import.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, "management-web")]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, "browser")]
public sealed class ImportCustomersKeyboardCancelTests : ManagementWebBrowserTestBase
{
    private readonly FakeCustomersApiClient customers = new();

    public ImportCustomersKeyboardCancelTests()
    {
        Registrations.AddSingleton<ICustomersApiClient>(customers);
    }

    [Fact]
    public async Task Keyboard_cancel_of_a_held_import_reports_canceled_and_focuses_the_status_region()
    {
        // Arrange
        customers.SetImportCustomersHandler(async (_, _, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ImportResultDto(0, 0);
        });
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, CustomerImportCsvHelpers.BuildCanonicalCsv("keyboard-cancel@import.test"));
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Preview" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Confirm Import" }).ClickAsync();
        var cancel = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel upload of customers.csv" });
        await Expect(cancel).ToBeVisibleAsync();

        // Act
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Back to Customers" }).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(Page.Locator("[data-upload-status]")).ToContainTextAsync("Canceled");
        await Expect(Page.Locator("section[aria-label='Customer import upload status']")).ToBeFocusedAsync();
    }
}
