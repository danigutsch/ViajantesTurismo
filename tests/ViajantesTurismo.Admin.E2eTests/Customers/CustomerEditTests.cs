using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Customers;

public class CustomerEditTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Customer_Edit_Renders_All_Sections_With_Populated_Data()
    {
        // Arrange
        var customer = await ApiClient.CreateCustomer(firstName: "Alice", lastName: "Owned");
        var companion = await ApiClient.CreateCustomer(firstName: "Bob", lastName: "Owned");
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var companionFullName = $"{companion.FirstName} {companion.LastName}";

        // Act
        await NavigateTo($"/customers/{customer.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        // Assert: all 8 section headers render.
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Personal Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Contact Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Identification" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Address" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Physical Information" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Accommodation Preferences" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Emergency Contact" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-title", new PageLocatorOptions { HasText = "Medical Information" })).ToBeVisibleAsync();

        // Assert: known owned/default values are populated.
        await Expect(Page.Locator("#firstName")).ToHaveValueAsync(customer.FirstName);
        await Expect(Page.Locator("#lastName")).ToHaveValueAsync(customer.LastName);
        await Expect(Page.Locator("#gender")).ToHaveValueAsync("Other");
        await Expect(Page.Locator("#nationality")).ToContainTextAsync("Brazil");
        await Expect(Page.Locator("#occupation")).ToHaveValueAsync("Tester");

        // Assert: generated contact fields are populated and identification controls render.
        await Expect(Page.Locator("#email")).ToHaveValueAsync(customer.Email);
        await Expect(Page.Locator("#mobile")).Not.ToHaveValueAsync(string.Empty);

        await Expect(Page.Locator("#nationalId")).Not.ToHaveValueAsync(string.Empty);
        await Expect(Page.Locator("#idNationality")).ToBeVisibleAsync();

        // Assert: address defaults are populated.
        await Expect(Page.Locator("#street")).ToHaveValueAsync("Test Street 1");
        await Expect(Page.Locator("#city")).ToHaveValueAsync("Test City");
        await Expect(Page.Locator("#country")).ToHaveValueAsync("Brazil");

        // Assert: companion combobox excludes the current customer and includes another owned customer.
        var companionSelect = Page.Locator("select[name=\"_model.AccommodationPreferences.CompanionId\"]");
        await Expect(companionSelect).ToBeVisibleAsync();
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = "-- No Companion --" })).ToBeAttachedAsync();
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = customerFullName })).Not.ToBeAttachedAsync();
        await Expect(companionSelect.Locator("option", new LocatorLocatorOptions { HasText = companionFullName })).ToBeAttachedAsync();

        // Assert: standard actions are visible.
        await Expect(Page.GetButton("Update Customer")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Cancel")).ToBeVisibleAsync();
    }
}
