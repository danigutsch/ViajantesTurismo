using Microsoft.AspNetCore.Components;
using PandaBear.Testing;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[TestScope(TestScope.Component)]
public class EditPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton<ICountryService>(new FakeCountryService());
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(new RecordingUiFailureReporter());
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Customer_edit_does_not_load_the_customer_collection_for_companion_selection()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        foreach (var index in Enumerable.Range(0, 800))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Fixture{index:D3}"));
        }

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, customer.Id));
        cut.WaitForElement("button:contains('Update Customer')");

        // Assert
        _fakeCustomersApi.LastGetCustomersMaxItems.ShouldBeNull();
    }

    [Fact]
    public async Task Companion_search_preserves_the_saved_selection_and_can_choose_a_customer_outside_the_initial_window()
    {
        // Arrange
        var retained = BuildCustomerDto(firstName: "Yara", lastName: "Retained", email: "retained@example.test");
        var replacement = BuildCustomerDto(firstName: "Zoe", lastName: "Target");
        var customer = BuildCustomerDetailsDto();
        customer = customer with
        {
            AccommodationPreferences = customer.AccommodationPreferences with
            {
                CompanionId = retained.Id
            }
        };
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id, firstName: "Zoe", lastName: "Target"));
        _fakeCustomersApi.AddCustomer(retained);
        _fakeCustomersApi.AddCustomer(replacement);
        foreach (var index in Enumerable.Range(0, 125))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Fixture{index:D3}"));
        }

        _fakeCustomersApi.SetUpdateCustomerOutcome(FakeApiClientOutcomes.TransportFailure());
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("#companionId").TextContent.ShouldContain("Yara Retained", StringComparison.Ordinal));
        var initialChoices = cut.FindAll("#companionId option").Count;

        // Act
        await cut.Find("#companionSearch").InputAsync(new ChangeEventArgs { Value = "Zoe Target" });
        var results = cut.FindAll("#companionId option").Select(option => option.GetAttribute("value")).ToArray();
        await cut.Find("#companionId").ChangeAsync(new ChangeEventArgs { Value = replacement.Id.ToString() });
        await cut.Find("form").SubmitAsync();

        // Assert
        initialChoices.ShouldBe(AdminContractConstants.MaxCustomerOptions + 2);
        results.ShouldContain(replacement.Id.ToString());
        results.ShouldNotContain(customer.Id.ToString());
        var update = _fakeCustomersApi.LastUpdatedCustomer.ShouldNotBeNull();
        update.AccommodationPreferences.CompanionId.ShouldBe(replacement.Id);
        _fakeCustomersApi.LastGetCustomersMaxItems.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Companion_search_failure_allows_keeping_or_clearing_the_saved_selection(bool clear)
    {
        // Arrange
        var retainedId = Guid.NewGuid();
        var customer = BuildCustomerDetailsDto();
        customer = customer with
        {
            AccommodationPreferences = customer.AccommodationPreferences with
            {
                CompanionId = retainedId
            }
        };
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.SetSearchCustomerOptionsHandler((_, _) =>
            Task.FromResult(FakeApiClientOutcomes.TransportFailure<GetCustomerOptionsDto>()));
        _fakeCustomersApi.SetUpdateCustomerOutcome(FakeApiClientOutcomes.TransportFailure());
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("You can keep or remove the selected companion.", StringComparison.Ordinal));
        var selectorEnabled = !cut.Find("#companionId").HasAttribute("disabled");
        var retainedValue = cut.FindAll("#companionId option")
            .ShouldHaveSingleItem(option => option.TextContent == "Selected companion").GetAttribute("value");

        // Act
        if (clear)
        {
            await cut.Find("#companionId").ChangeAsync(new ChangeEventArgs { Value = string.Empty });
        }

        await cut.Find("form").SubmitAsync();

        // Assert
        selectorEnabled.ShouldBeTrue();
        retainedValue.ShouldBe(retainedId.ToString());
        var update = _fakeCustomersApi.LastUpdatedCustomer.ShouldNotBeNull();
        update.AccommodationPreferences.CompanionId.ShouldBe(clear ? null : retainedId);
    }

    [Fact]
    public void Renders_loading_state()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customerId));

        // Assert
        var showsInitialState = cut.Markup.Contains("Loading...", StringComparison.Ordinal)
                || cut.Markup.Contains("Customer not found.", StringComparison.Ordinal);
        cut.WaitForAssertion(() =>
            showsInitialState.ShouldBeTrue());
    }

    [Fact]
    public async Task OnInitializedAsync_when_transport_fails_shows_a_safe_error_message()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _fakeCustomersApi.SetGetCustomerByIdOutcome(FakeApiClientOutcomes.TransportFailure<CustomerDetailsDto>());

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customerId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldBe("We couldn't load the customer right now. Please try again.");
            cut.FindAll("form").ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task HandleSubmit_when_transport_fails_shows_a_safe_error_message()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.SetUpdateCustomerOutcome(FakeApiClientOutcomes.TransportFailure());

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldBe("We couldn't update the customer right now. Please try again.");
        });
    }

    [Fact]
    public async Task Loads_and_displays_customer_data()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var firstNameInput = cut.Find("input#firstName");
            firstNameInput.GetAttribute("value").ShouldBe(customer.PersonalInfo.FirstName);

            var lastNameInput = cut.Find("input#lastName");
            lastNameInput.GetAttribute("value").ShouldBe(customer.PersonalInfo.LastName);

            var emailInput = cut.Find("input#email");
            emailInput.GetAttribute("value").ShouldBe(customer.ContactInfo.Email);
        });
    }

    [Fact]
    public async Task Renders_personal_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Personal Information", StringComparison.Ordinal));
            card.QuerySelector("input#firstName").ShouldNotBeNull();
            card.QuerySelector("input#lastName").ShouldNotBeNull();
            card.QuerySelector("input#birthDate").ShouldNotBeNull();
            card.QuerySelector("input#gender").ShouldNotBeNull();
            card.QuerySelector("#nationality").ShouldNotBeNull();
            card.QuerySelector("input#occupation").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Renders_contact_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Contact Information", StringComparison.Ordinal));
            card.QuerySelector("input#email").ShouldNotBeNull();
            card.QuerySelector("input#mobile").ShouldNotBeNull();
            card.QuerySelector("input#instagram").ShouldNotBeNull();
            card.QuerySelector("input#facebook").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Renders_identification_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Identification", StringComparison.Ordinal));
            card.QuerySelector("input#nationalId").ShouldNotBeNull();
            card.QuerySelector("#idNationality").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Renders_address_card_with_all_fields()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Address", StringComparison.Ordinal));
            card.QuerySelector("input#street").ShouldNotBeNull();
            card.QuerySelector("input#complement").ShouldNotBeNull();
            card.QuerySelector("input#neighborhood").ShouldNotBeNull();
            card.QuerySelector("input#postalCode").ShouldNotBeNull();
            card.QuerySelector("input#city").ShouldNotBeNull();
            card.QuerySelector("input#state").ShouldNotBeNull();
            card.QuerySelector("#country").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Renders_physical_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Physical Information", StringComparison.Ordinal));

            var weightInput = card.QuerySelector("input#weight");
            _ = weightInput.ShouldNotBeNull();
            card.TextContent.ShouldContain("Weight (kg)", StringComparison.Ordinal);

            var heightInput = card.QuerySelector("input#height");
            _ = heightInput.ShouldNotBeNull();
            card.TextContent.ShouldContain("Height (cm)", StringComparison.Ordinal);

            var bikeTypeSelect = card.QuerySelector("select#bikeType");
            _ = bikeTypeSelect.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task BikeType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bikeTypeSelect = cut.Find("select#bikeType");
            var options = bikeTypeSelect.QuerySelectorAll("option");
            options.Length.ShouldBe(3);
            options[0].TextContent.ShouldContain("None", StringComparison.Ordinal);
            options[1].TextContent.ShouldContain("Regular", StringComparison.Ordinal);
            options[2].TextContent.ShouldContain("E-Bike", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_accommodation_preferences_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Accommodation Preferences", StringComparison.Ordinal));
            card.QuerySelector("select#roomType").ShouldNotBeNull();
            card.QuerySelector("select#bedType").ShouldNotBeNull();
            card.TextContent.ShouldContain("Companion", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task RoomType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var roomTypeSelect = cut.Find("select#roomType");
            var options = roomTypeSelect.QuerySelectorAll("option");
            options.Length.ShouldBe(2);
            options[0].TextContent.ShouldContain("Double Room", StringComparison.Ordinal);
            options[1].TextContent.ShouldContain("Single Room", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task BedType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bedTypeSelect = cut.Find("select#bedType");
            var options = bedTypeSelect.QuerySelectorAll("option");
            options.Length.ShouldBe(2);
            options[0].TextContent.ShouldContain("Single Bed", StringComparison.Ordinal);
            options[1].TextContent.ShouldContain("Double Bed", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_emergency_contact_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Emergency Contact", StringComparison.Ordinal));
            card.QuerySelector("input#emergencyName").ShouldNotBeNull();
            card.QuerySelector("input#emergencyMobile").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Renders_medical_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Medical Information", StringComparison.Ordinal));

            var allergiesTextArea = card.QuerySelector("textarea#allergies");
            _ = allergiesTextArea.ShouldNotBeNull();
            allergiesTextArea.GetAttribute("rows").ShouldBe("3");

            var additionalInfoTextArea = card.QuerySelector("textarea#additionalInfo");
            _ = additionalInfoTextArea.ShouldNotBeNull();
            additionalInfoTextArea.GetAttribute("rows").ShouldBe("3");
        });
    }

    [Fact]
    public async Task Can_cancel_redirect_after_update()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Act
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Redirecting to details page", StringComparison.Ordinal));
        var cancelButton = cut.Find(".alert.alert-info button");
        await cut.InvokeAsync(async () => await cancelButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alerts = cut.FindAll(".alert.alert-success");
            var cancelledAlert = alerts.Last(a => a.TextContent.Contains("Customer updated successfully", StringComparison.Ordinal));
            cancelledAlert.TextContent.ShouldContain("Customer updated successfully", StringComparison.Ordinal);

            var goToDetailsButton = cancelledAlert.QuerySelector("button");
            _ = goToDetailsButton.ShouldNotBeNull();
            goToDetailsButton.TextContent.ShouldContain("Go to Details", StringComparison.Ordinal);
        });
    }
}
