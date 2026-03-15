namespace ViajantesTurismo.Admin.E2ETests.Customers;

public class CustomerTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Complete_Wizard_View_Details_And_Edit_Customer()
    {
        // Arrange
        var uid = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"E2E{uid}";
        var lastName = $"Customer{uid}";
        var nationalId = $"E2E{uid}";
        var email = $"e2e-wizard-{uid}@test.com";
        var street = $"E2E Street {uid}";
        var emergencyContactName = $"EmergencyPerson {uid}";

        // Act
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

        await Expect(Page).ToHaveTitleAsync("Create Customer - Identification");
        await Expect(Page.GetByText("Step 2 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#nationalId", nationalId);

        // CountrySelector for ID Nationality has no explicit id; locate by label context
        var idNatField = Page.Locator(".mb-3").Filter(new LocatorFilterOptions { HasText = "ID Nationality" });
        await idNatField.Locator("button.form-select").ClickAsync();
        await Page.Locator(".country-dropdown-menu input").FillAsync("Brazil");
        await Page.Locator(".country-dropdown-item", new PageLocatorOptions { HasText = "Brazil" }).First.ClickAsync();

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Contact Information");
        await Expect(Page.GetByText("Step 3 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#email", email);
        await Page.FillAsync("#mobile", "+5511999990001");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Address");
        await Expect(Page.GetByText("Step 4 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#street", street);
        await Page.FillAsync("#neighborhood", "TestVille");
        await Page.FillAsync("#postalCode", "54321-000");
        await Page.FillAsync("#city", "E2ECity");
        await Page.FillAsync("#state", "TS");
        await Page.FillAsync("#country", "Brazil");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Physical Information");
        await Expect(Page.GetByText("Step 5 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#weightKg", "65");
        await Page.FillAsync("#heightCm", "170");
        await Page.SelectOptionAsync("#bikeType", "Regular");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Accommodation Preferences");
        await Expect(Page.GetByText("Step 6 of 8")).ToBeVisibleAsync();

        await Page.SelectOptionAsync("#roomType", "SingleOccupancy");
        await Page.SelectOptionAsync("#bedType", "DoubleBed");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Emergency Contact");
        await Expect(Page.GetByText("Step 7 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#name", emergencyContactName);
        await Page.FillAsync("#mobile", "+5511988880001");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Medical Information");
        await Expect(Page.GetByText("Step 8 of 8")).ToBeVisibleAsync();

        await Page.FillAsync("#allergies", "None known");
        await Page.FillAsync("#additionalInfo", "E2E test medical info");

        await Page.GetButton("Review & Submit").ClickAsync();

        // Assert
        await Expect(Page).ToHaveTitleAsync("Create Customer - Review & Submit");

        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Brazil").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText(email)).ToBeVisibleAsync();
        await Expect(Page.GetByText(street)).ToBeVisibleAsync();
        await Expect(Page.GetByText(emergencyContactName)).ToBeVisibleAsync();
        await Expect(Page.GetByText("None known")).ToBeVisibleAsync();

        // Act
        await Page.GetButton("Create Customer").ClickAsync();

        // Assert
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText(email)).ToBeVisibleAsync();
        await Expect(Page.GetByText(street)).ToBeVisibleAsync();
        await Expect(Page.GetByText(emergencyContactName)).ToBeVisibleAsync();

        var detailUrl = Page.Url;
        var customerId = detailUrl.Split('/').Last();

        // Act
        await NavigateTo($"/customers/{customerId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        await Page.FillAsync("#occupation", "");
        await Page.FillAsync("#occupation", "Senior QA Engineer");
        await Page.FillAsync("#mobile", "");
        await Page.FillAsync("#mobile", "+5511999990099");

        await Page.GetButton("Update Customer").ClickAsync();

        // Assert
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();

        await Page.CancelTimedRedirect();

        await NavigateTo($"/customers/{customerId}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText("Senior QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText("+5511999990099")).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText($"{firstName} {lastName}")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Senior QA Engineer")).ToBeVisibleAsync();
        await Expect(Page.GetByText("+5511999990099")).ToBeVisibleAsync();
    }
}
