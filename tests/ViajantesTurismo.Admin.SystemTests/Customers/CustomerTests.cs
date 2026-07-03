namespace ViajantesTurismo.Admin.SystemTests.Customers;

public class CustomerTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private const float WizardStepTransitionTimeoutMilliseconds = 15000;

    [Fact]
    public async Task Can_complete_wizard_view_details_and_edit_customer()
    {
        // Arrange
        var uid = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"E2E{uid}";
        var lastName = $"Customer{uid}";
        var nationalId = $"E2E{uid}";
        var email = $"e2e-wizard-{uid}@test.com";
        var mobile = "+5511999990001";
        var street = $"E2E Street {uid}";
        var emergencyContactName = $"EmergencyPerson {uid}";

        // Act
        await NavigateTo("/customers/create/personal-info");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Personal Information");
        await Expect(Page.GetByText("Step 1 of 8")).ToBeVisibleAsync();

        await Page.Locator("#firstName").FillAndExpectValue(firstName);
        await Page.Locator("#lastName").FillAndExpectValue(lastName);
        await Page.Locator("#birthDate").FillAndExpectValue("1990-06-15");
        var genderInput = Page.Locator("#gender");
        await genderInput.SelectOptionAsync("Female");
        await Expect(genderInput).ToHaveValueAsync("Female");

        // CountrySelector: click to open, search, select
        await Page.Locator("#nationality").ClickAsync();
        await Page.Locator(".country-dropdown-menu input").FillAsync("Brazil");
        await Page.Locator(".country-dropdown-item", new PageLocatorOptions { HasText = "Brazil" }).First.ClickAsync();

        await Page.Locator("#occupation").FillAndExpectValue("QA Engineer");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Identification");
        await Expect(Page.GetByText("Step 2 of 8")).ToBeVisibleAsync();

        var nationalIdInput = Page.Locator("#nationalId");
        await nationalIdInput.FillAndExpectValue(nationalId);
        await Expect(nationalIdInput).ToHaveValueAsync(nationalId);

        await Page.Locator("#idNationality").ClickAsync();
        await Page.Locator(".country-dropdown-menu input").FillAsync("Brazil");
        await Page.Locator(".country-dropdown-item", new PageLocatorOptions { HasText = "Brazil" }).First.ClickAsync();

        await nationalIdInput.FillAndExpectValue(nationalId);
        await Expect(nationalIdInput).ToHaveValueAsync(nationalId);

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveURLAsync(CustomerTestRegexes.ContactStep(), new() { Timeout = WizardStepTransitionTimeoutMilliseconds });
        await Expect(Page.Locator("#email")).ToBeVisibleAsync(new() { Timeout = WizardStepTransitionTimeoutMilliseconds });
        await Expect(Page.GetByText("Step 3 of 8")).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync("Create Customer - Contact Information");

        await Page.Locator("#email").FillAndExpectValue(email);
        await Page.Locator("#mobile").FillAndExpectValue(mobile);
        await Expect(Page.Locator("#email")).ToHaveValueAsync(email);
        await Expect(Page.Locator("#mobile")).ToHaveValueAsync(mobile);

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Address");
        await Expect(Page.GetByText("Step 4 of 8")).ToBeVisibleAsync();

        await Page.Locator("#street").FillAndExpectValue(street);
        await Page.Locator("#neighborhood").FillAndExpectValue("TestVille");
        await Page.Locator("#postalCode").FillAndExpectValue("54321-000");
        await Page.Locator("#city").FillAndExpectValue("E2ECity");
        await Page.Locator("#state").FillAndExpectValue("TS");
        await Page.Locator("#country").FillAndExpectValue("Brazil");
        await Page.Locator("#country").BlurAsync();
        await Expect(Page.Locator("#street")).ToHaveValueAsync(street);
        await Expect(Page.Locator("#neighborhood")).ToHaveValueAsync("TestVille");
        await Expect(Page.Locator("#postalCode")).ToHaveValueAsync("54321-000");
        await Expect(Page.Locator("#city")).ToHaveValueAsync("E2ECity");
        await Expect(Page.Locator("#state")).ToHaveValueAsync("TS");
        await Expect(Page.Locator("#country")).ToHaveValueAsync("Brazil");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Physical Information");
        await Expect(Page.GetByText("Step 5 of 8")).ToBeVisibleAsync();

        var weightInput = Page.Locator("#weightKg");
        var heightInput = Page.Locator("#heightCm");
        await weightInput.FillAndExpectValue("65");
        await heightInput.FillAndExpectValue("170");
        await heightInput.BlurAsync();
        var bikeTypeInput = Page.Locator("#bikeType");
        await bikeTypeInput.SelectOptionAsync("Regular");
        await Expect(weightInput).ToHaveValueAsync("65");
        await Expect(heightInput).ToHaveValueAsync("170");
        await Expect(bikeTypeInput).ToHaveValueAsync("Regular");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Accommodation Preferences");
        await Expect(Page.GetByText("Step 6 of 8")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Loading customers...")).ToBeHiddenAsync();

        var roomTypeInput = Page.Locator("#roomType");
        await roomTypeInput.SelectOptionAsync("SingleOccupancy");
        await Expect(roomTypeInput).ToHaveValueAsync("SingleOccupancy");

        var bedTypeInput = Page.Locator("#bedType");
        await bedTypeInput.SelectOptionAsync("DoubleBed");
        await Expect(bedTypeInput).ToHaveValueAsync("DoubleBed");

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Emergency Contact");
        await Expect(Page.GetByText("Step 7 of 8")).ToBeVisibleAsync();

        var emergencyStep = Page.Locator("article").Filter(new LocatorFilterOptions { HasText = "Step 7 of 8: Emergency Contact" });
        var emergencyNameInput = emergencyStep.Locator("#name");
        var emergencyMobileInput = emergencyStep.Locator("#mobile");
        var emergencyMobileNumber = "+5511988880001";
        await emergencyNameInput.FillAndExpectValue(emergencyContactName);
        await emergencyMobileInput.FillAndExpectValue(emergencyMobileNumber);
        await emergencyNameInput.FillAndExpectValue(emergencyContactName);
        await Expect(emergencyNameInput).ToHaveValueAsync(emergencyContactName);
        await Expect(emergencyMobileInput).ToHaveValueAsync(emergencyMobileNumber);

        await Page.GetButton("Next").ClickAsync();

        await Expect(Page).ToHaveTitleAsync("Create Customer - Medical Information");
        await Expect(Page.GetByText("Step 8 of 8")).ToBeVisibleAsync();

        var allergiesInput = Page.Locator("#allergies");
        var additionalInfoInput = Page.Locator("#additionalInfo");

        await allergiesInput.FillAndExpectValue("None known");
        await allergiesInput.BlurAsync();
        await additionalInfoInput.FillAndExpectValue("E2E test medical info");
        await additionalInfoInput.BlurAsync();
        await Expect(allergiesInput).ToHaveValueAsync("None known");
        await Expect(additionalInfoInput).ToHaveValueAsync("E2E test medical info");

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
        var detailUrlSegments = detailUrl.Split('/');
        var customerId = detailUrlSegments[^1];

        // Act
        await NavigateTo($"/customers/{customerId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        await Page.Locator("#occupation").FillAndExpectValue("");
        await Page.Locator("#occupation").FillAndExpectValue("Senior QA Engineer");
        await Page.Locator("#mobile").FillAndExpectValue("");
        await Page.Locator("#mobile").FillAndExpectValue("+5511999990099");
        await Page.Locator("#mobile").BlurAsync();
        await Expect(Page.Locator("#occupation")).ToHaveValueAsync("Senior QA Engineer");
        await Expect(Page.Locator("#mobile")).ToHaveValueAsync("+5511999990099");

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
