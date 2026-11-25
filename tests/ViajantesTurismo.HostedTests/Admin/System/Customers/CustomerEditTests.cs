using SharedKernel.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using SharedKernel.Testing.Playwright;
namespace ViajantesTurismo.HostedTests.Admin.System.Customers;

[TestScope(TestScope.System)]
public class CustomerEditTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Customer_edit_renders_all_sections_with_populated_data()
    {
        // Arrange
        var ownedFamilyName = $"Owned{Guid.NewGuid():N}";
        var customer = await ApiClient.CreateCustomer(firstName: "Alice", lastName: ownedFamilyName);
        var companion = await ApiClient.CreateCustomer(firstName: "Bob", lastName: ownedFamilyName);
        var sameNameCustomer = await ApiClient.CreateCustomer(firstName: "Alice", lastName: ownedFamilyName);

        // Act
        await NavigateTo($"/customers/{customer.Id}/edit");
        await Page.ShouldHaveTitle("Edit Customer");

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
        await Expect(Page.Locator("#country")).ToContainTextAsync("Brazil");

        // Arrange: use the companion picker's bounded search.
        var companionSearch = Page.GetByRole(AriaRole.Searchbox, new()
        {
            Name = "Search for a companion",
            Exact = true
        });
        var companionSelect = Page.GetByRole(AriaRole.Combobox, new()
        {
            Name = "Companion (Optional)",
            Exact = true
        });

        // Act
        await companionSearch.FillAsync(customer.Id.ToString("D"));

        // Assert
        await Expect(Page.GetByText("No customers match.", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();

        // Act
        await companionSearch.FillAsync(ownedFamilyName);

        // Assert: the edited customer is excluded while both eligible records remain available.
        await Expect(companionSelect).ToBeVisibleAsync();
        await Expect(companionSelect.Locator("option[value='']")).ToHaveTextAsync("-- No Companion --");
        await Expect(companionSelect.Locator($"option[value='{sameNameCustomer.Id:D}']")).ToBeAttachedAsync();
        await Expect(companionSelect.Locator($"option[value='{companion.Id:D}']")).ToBeAttachedAsync();
        await Expect(companionSelect.Locator($"option[value='{customer.Id:D}']")).Not.ToBeAttachedAsync();

        // Assert: standard actions are visible.
        await Expect(Page.GetButton("Update Customer")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Cancel")).ToBeVisibleAsync();
    }
}
