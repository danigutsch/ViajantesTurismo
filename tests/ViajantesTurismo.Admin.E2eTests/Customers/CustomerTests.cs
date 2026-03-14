using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Customers;

public class CustomerTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Complete_Wizard_View_Details_And_Edit_Customer()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"E2E{uid}";
        var lastName = $"Customer{uid}";
        var nationalId = $"E2E{uid}";
        var email = $"e2e-wizard-{uid}@test.com";
        var street = $"E2E Street {uid}";
        var emergencyContactName = $"EmergencyPerson {uid}";

        // === Step 1: Personal Information ===
        await NavigateTo("/customers/create/personal-info");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Personal Information");
        await Expect(Page.GetByText("Step 1 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#firstName", firstName);
        await Page.FillAsync("#lastName", lastName);
        await Page.FillAsync("#birthDate", "1990-06-15");
        await Page.SelectOptionAsync("#gender", "Female");

        // CountrySelector: click to open, search, select
        await Page.Locator("#nationality").ClickAsync();
        await Page.Locator(".country-dropdown-menu input").FillAsync("Brazil");
        await Page.Locator(".country-dropdown-item", new PageLocatorOptions { HasText = "Brazil" }).First.ClickAsync();

        await Page.FillAsync("#occupation", "QA Engineer");

        await Page.GetButton("Next").ClickAsync();

        // === Step 2: Identification ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Identification");
        await Expect(Page.GetByText("Step 2 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#nationalId", nationalId);

        // CountrySelector for ID Nationality has no explicit id; locate by label context
        var idNatField = Page.Locator(".mb-3").Filter(new LocatorFilterOptions { HasText = "ID Nationality" });
        await idNatField.Locator("button.form-select").ClickAsync();
        await Page.Locator(".country-dropdown-menu input").FillAsync("Brazil");
        await Page.Locator(".country-dropdown-item", new PageLocatorOptions { HasText = "Brazil" }).First.ClickAsync();

        await Page.GetButton("Next").ClickAsync();

        // === Step 3: Contact ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Contact Information");
        await Expect(Page.GetByText("Step 3 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#email", email);
        await Page.FillAsync("#mobile", "+5511999990001");

        await Page.GetButton("Next").ClickAsync();

        // === Step 4: Address ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Address");
        await Expect(Page.GetByText("Step 4 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#street", street);
        await Page.FillAsync("#neighborhood", "TestVille");
        await Page.FillAsync("#postalCode", "54321-000");
        await Page.FillAsync("#city", "E2ECity");
        await Page.FillAsync("#state", "TS");
        await Page.FillAsync("#country", "Brazil");

        await Page.GetButton("Next").ClickAsync();

        // === Step 5: Physical ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Physical Information");
        await Expect(Page.GetByText("Step 5 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#weightKg", "65");
        await Page.FillAsync("#heightCm", "170");
        await Page.SelectOptionAsync("#bikeType", "Regular");

        await Page.GetButton("Next").ClickAsync();

        // === Step 6: Accommodation ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Accommodation Preferences");
        await Expect(Page.GetByText("Step 6 of 8")).ToBeVisibleAsync();

        await Page.SelectOptionAsync("#roomType", "SingleOccupancy");
        await Page.SelectOptionAsync("#bedType", "DoubleBed");

        await Page.GetButton("Next").ClickAsync();

        // === Step 7: Emergency Contact ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Emergency Contact");
        await Expect(Page.GetByText("Step 7 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#name", emergencyContactName);
        await Page.FillAsync("#mobile", "+5511988880001");

        await Page.GetButton("Next").ClickAsync();

        // === Step 8: Medical ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Medical Information");
        await Expect(Page.GetByText("Step 8 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#allergies", "None known");
        await Page.FillAsync("#additionalInfo", "E2E test medical info");

        await Page.GetButton("Review & Submit").ClickAsync();

        // === Review Step ===
        await Expect(Page).ToHaveTitleAsync("Create Customer - Review & Submit");

        // Verify review shows data from all steps
        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Brazil").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText(email)).ToBeVisibleAsync();
        await Expect(Page.GetByText(street)).ToBeVisibleAsync();
        await Expect(Page.GetByText(emergencyContactName)).ToBeVisibleAsync();
        await Expect(Page.GetByText("None known")).ToBeVisibleAsync();

        // Submit
        await Page.GetButton("Create Customer").ClickAsync();

        // === Redirected to Customer Details ===
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        // Verify key details
        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText(email)).ToBeVisibleAsync();
        await Expect(Page.GetByText(street)).ToBeVisibleAsync();
        await Expect(Page.GetByText(emergencyContactName)).ToBeVisibleAsync();

        // Extract customer ID from URL
        var detailUrl = Page.Url;
        var customerId = detailUrl.Split('/').Last();

        // === Edit Customer ===
        await NavigateTo($"/customers/{customerId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        // Update occupation and mobile
        await Page.FillAsync("#occupation", "");
        await Page.FillAsync("#occupation", "Senior QA Engineer");
        await Page.FillAsync("#mobile", "");
        await Page.FillAsync("#mobile", "+5511999990099");

        await Page.GetButton("Update Customer").ClickAsync();

        // Success message
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();

        // Cancel auto-redirect to verify details manually
        var cancelButton = Page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
        if (await cancelButton.CountAsync() > 0)
        {
            await cancelButton.ClickAsync();
        }

        // === Verify edits on details page ===
        await NavigateTo($"/customers/{customerId}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText("Senior QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText("+5511999990099")).ToBeVisibleAsync();

        // === Refresh persistence: hard reload and verify saved values survive ===
        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Senior QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText("+5511999990099")).ToBeVisibleAsync();
    }
}
