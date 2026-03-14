using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Customers;

public class CustomerEditTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Customer_Edit_Renders_All_Sections_With_Populated_Data()
    {
        // Navigate to the customers list and open Alice Smith's edit page
        await NavigateToAsync("/customers");
        var aliceRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "Alice Smith" });
        await aliceRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        // === All 8 section headers rendered ===
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Personal Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Contact Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Identification" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Address" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Physical Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Accommodation Preferences" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Emergency Contact" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Medical Information" })).ToBeVisibleAsync();

        // === Personal Information fields populated ===
        await Expect(Page.Locator("#firstName")).ToHaveValueAsync("Alice");
        await Expect(Page.Locator("#lastName")).ToHaveValueAsync("Smith");
        await Expect(Page.Locator("#gender")).ToHaveValueAsync("Female");
        await Expect(Page.Locator("#nationality")).ToContainTextAsync("Brazil");
        await Expect(Page.Locator("#occupation")).ToHaveValueAsync("Engineer");

        // === Contact Information ===
        await Expect(Page.Locator("#email")).ToHaveValueAsync("alice@example.com");
        await Expect(Page.Locator("#mobile")).ToHaveValueAsync("+5511999999999");

        // === Identification ===
        await Expect(Page.Locator("#nationalId")).ToHaveValueAsync("123456789");
        await Expect(Page.Locator("#idNationality")).ToContainTextAsync("Brazil");

        // === Address ===
        await Expect(Page.Locator("#street")).ToHaveValueAsync("Rua A, 123");
        await Expect(Page.Locator("#city")).ToHaveValueAsync("São Paulo");
        await Expect(Page.Locator("#country")).ToHaveValueAsync("Brazil");

        // === Companion combobox excludes the current customer ===
        var companionSelect = Page.Locator("select[name=\"_model.AccommodationPreferences.CompanionId\"]");
        await Expect(companionSelect).ToBeVisibleAsync();
        // "No Companion" option should exist
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = "-- No Companion --" })).ToBeAttachedAsync();
        // Alice should NOT appear in the companion dropdown
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = "Alice Smith" })).Not.ToBeAttachedAsync();
        // Another customer should be present
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = "Bob Johnson" })).ToBeAttachedAsync();

        // === Update and Cancel buttons ===
        await Expect(Page.GetButton("Update Customer")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Cancel")).ToBeVisibleAsync();
    }
}
